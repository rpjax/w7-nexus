using Aidan.Core.Errors;
using Nexus.Payments.Application.Services.Contracts;
using Aidan.Core.Patterns;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Nexus.Payments.Application.Models;
using Nexus.Payments.Presentation;
using Nexus.Payments.Errors;

namespace Nexus.Payments.Infrastructure.Notifications;

public sealed class SignalRPaymentNotifier : IPaymentNotifier
{
    public const string ClientMethodName = "PaymentStatusChanged";

    private readonly IHubContext<PaymentStatusHub> _hubContext;
    private readonly ILogger<SignalRPaymentNotifier> _logger;

    public SignalRPaymentNotifier(
        IHubContext<PaymentStatusHub> hubContext,
        ILogger<SignalRPaymentNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<IResult> NotifyStatusChangedAsync(NotifyStatusChangedRequest request)
    {
        var paymentId = request.PaymentId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("O ID do pagamento é obrigatório.")
                .Build());
        }

        var groupName = PaymentStatusHub.GroupNameForPayment(paymentId);
        var payload = new PaymentStatusChangedNotification(paymentId, request.Status);

        try
        {
            await _hubContext.Clients.Group(groupName).SendAsync(ClientMethodName, payload);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR: failed to publish payment status for {PaymentId}.", paymentId);
            return Result.Failure(Error.Create()
                .WithCode("PaymentNotifier.SIGNALR_PUBLISH_FAILED")
                .WithMessage("Não foi possível notificar os clientes em tempo real.")
                .Build());
        }
    }
}
