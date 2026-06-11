using System.Text.Json;
using Nexus.Payments.Application.Contracts;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Microsoft.Extensions.Logging;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application;

namespace Nexus.Payments.Application;

public sealed class GatewayPaymentWebhookService : IGatewayPaymentWebhookService
{
    private static readonly JsonSerializerOptions FrendzJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static readonly JsonSerializerOptions StandardJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<GatewayPaymentWebhookService> _logger;

    public GatewayPaymentWebhookService(
        IPaymentRepository paymentRepository,
        IPaymentService paymentService,
        ILogger<GatewayPaymentWebhookService> logger)
    {
        _paymentRepository = paymentRepository;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task ProcessFrendzPostbackAsync(string jsonBody, CancellationToken cancellationToken = default)
    {
        FrendzPostbackDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<FrendzPostbackDto>(jsonBody, FrendzJsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Frendz webhook: invalid JSON.");
            return;
        }

        if (dto is null || string.IsNullOrWhiteSpace(dto.TransactionHash))
        {
            _logger.LogWarning("Frendz webhook: missing transaction_hash.");
            return;
        }

        var hash = dto.TransactionHash.Trim();
        var payment = await _paymentRepository.AsQueryable()
            .Where(p => p.Gateway == PaymentGateway.Frendz && p.GatewayTransactionId == hash)
            .FirstOrDefaultAsync();

        if (payment is null)
        {
            _logger.LogWarning("Frendz webhook: no payment for transaction_hash {Hash}.", hash);
            return;
        }

        var status = dto.Status?.Trim().ToLowerInvariant() ?? "";
        await ApplyFrendzStatusAsync(payment.Id, status);
    }

    public async Task ProcessStandardGatewayWebhookAsync(
        PaymentGateway gateway,
        string jsonBody,
        CancellationToken cancellationToken = default)
    {
        if (gateway is not PaymentGateway.SigiloPay and not PaymentGateway.Wintech)
        {
            _logger.LogWarning("Standard webhook processor invoked for unsupported gateway {Gateway}.", gateway);
            return;
        }

        StandardWebhookDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<StandardWebhookDto>(jsonBody, StandardJsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "{Gateway} webhook: invalid JSON.", gateway);
            return;
        }

        if (dto is null)
            return;

        var ev = dto.Event?.Trim().ToUpperInvariant() ?? "";
        var tx = dto.Transaction;

        if (string.IsNullOrWhiteSpace(ev) && tx?.Status is null)
        {
            _logger.LogWarning("{Gateway} webhook: missing event and transaction status.", gateway);
            return;
        }

        if (ev == "TRANSACTION_CREATED")
            return;

        if (tx is null)
        {
            _logger.LogWarning("{Gateway} webhook: missing transaction object.", gateway);
            return;
        }

        var payment = await ResolveStandardPaymentAsync(gateway, tx);
        if (payment is null)
        {
            _logger.LogWarning(
                "{Gateway} webhook: payment not found (transaction id {TxId}, identifier {Identifier}).",
                gateway,
                tx?.Id,
                tx?.Identifier);
            return;
        }

        await ApplyStandardLifecycleAsync(payment.Id, ev, tx?.Status);
    }

    private async Task<Payment?> ResolveStandardPaymentAsync(PaymentGateway gateway, StandardWebhookTransactionDto? tx)
    {
        if (tx is null)
            return null;

        var identifier = tx.Identifier?.Trim();
        var gatewayId = tx.Id?.Trim();

        if (!string.IsNullOrWhiteSpace(identifier))
        {
            var byId = await _paymentRepository.AsQueryable()
                .Where(p => p.Gateway == gateway && p.Id == identifier)
                .FirstOrDefaultAsync();
            if (byId is not null)
                return byId;
        }

        if (!string.IsNullOrWhiteSpace(gatewayId))
        {
            return await _paymentRepository.AsQueryable()
                .Where(p => p.Gateway == gateway && p.GatewayTransactionId == gatewayId)
                .FirstOrDefaultAsync();
        }

        return null;
    }

    private async Task ApplyFrendzStatusAsync(string paymentId, string status)
    {
        switch (status)
        {
            case "paid":
            case "completed":
                LogResult(paymentId, await _paymentService.PayAsync(paymentId), "Frendz Pay");
                return;
            case "refunded":
                LogResult(paymentId, await _paymentService.RefundAsync(paymentId), "Frendz Refund");
                return;
            case "canceled":
            case "cancelled":
            case "failed":
            case "expired":
            case "refused":
                LogResult(
                    paymentId,
                    await _paymentService.KillAsync(paymentId, $"Frendz webhook status: {status}"),
                    "Frendz Kill");
                return;
            default:
                if (string.IsNullOrEmpty(status))
                    _logger.LogWarning("Frendz webhook: empty status for payment {PaymentId}.", paymentId);
                else
                    _logger.LogInformation("Frendz webhook: ignored status {Status} for payment {PaymentId}.", status, paymentId);
                return;
        }
    }

    private async Task ApplyStandardLifecycleAsync(string paymentId, string eventName, string? transactionStatus)
    {
        var st = transactionStatus?.Trim().ToUpperInvariant() ?? "";

        if (eventName == "TRANSACTION_REFUNDED" || st == "REFUNDED")
        {
            LogResult(paymentId, await _paymentService.RefundAsync(paymentId), "Standard Refund");
            return;
        }

        if (eventName == "TRANSACTION_CANCELED"
            || st is "FAILED" or "CHARGED_BACK")
        {
            var reason = string.IsNullOrWhiteSpace(eventName) ? $"status:{st}" : $"{eventName}/{st}".Trim('/');
            LogResult(paymentId, await _paymentService.KillAsync(paymentId, reason), "Standard Kill");
            return;
        }

        var payEligible = st == "COMPLETED"
            || (eventName == "TRANSACTION_PAID" && st != "PENDING");

        if (payEligible)
        {
            LogResult(paymentId, await _paymentService.PayAsync(paymentId), "Standard Pay");
            return;
        }

        if (st == "PENDING" || string.IsNullOrEmpty(st))
            return;

        _logger.LogInformation(
            "Gateway webhook: no lifecycle action for payment {PaymentId} (event {Event}, status {Status}).",
            paymentId,
            eventName,
            st);
    }

    private void LogResult(string paymentId, IResult result, string operation)
    {
        if (result.IsSuccess)
            return;

        _logger.LogWarning(
            "{Operation} failed for payment {PaymentId}: {Errors}",
            operation,
            paymentId,
            string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Message}")));
    }

    private sealed class FrendzPostbackDto
    {
        public string? TransactionHash { get; init; }
        public string? Status { get; init; }
    }

    private sealed class StandardWebhookDto
    {
        public string? Event { get; init; }
        public StandardWebhookTransactionDto? Transaction { get; init; }
    }

    private sealed class StandardWebhookTransactionDto
    {
        public string? Id { get; init; }
        public string? Identifier { get; init; }
        public string? Status { get; init; }
    }
}
