using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Microsoft.Extensions.Options;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Application.Options;
using Nexus.Gateways.Application.Requests;
using Nexus.Gateways.Application.Responses;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Payments.Errors;

namespace Nexus.Gateways.Application.Services;

public sealed class GatewayOrchestrator : IGatewayOrchestrator
{
    private IFrendzApiCredentialsRepository _frendzCredentialsRepository { get; }
    private ISigiloPayApiCredentialsRepository _sigiloPayCredentialsRepository { get; }
    private IWintechApiCredentialsRepository _wintechCredentialsRepository { get; }
    private IFrendzServiceFactory _frendzServiceFactory { get; }
    private ISigiloPayServiceFactory _sigiloPayServiceFactory { get; }
    private IWintechServiceFactory _wintechServiceFactory { get; }
    private GatewaysOptions _options { get; }
    private ILogger<GatewayOrchestrator> _logger { get; }

    public GatewayOrchestrator(
        IFrendzApiCredentialsRepository frendzCredentialsRepository,
        ISigiloPayApiCredentialsRepository sigiloPayCredentialsRepository,
        IWintechApiCredentialsRepository wintechCredentialsRepository,
        IFrendzServiceFactory frendzServiceFactory,
        ISigiloPayServiceFactory sigiloPayServiceFactory,
        IWintechServiceFactory wintechServiceFactory,
        IOptions<GatewaysOptions> options,
        ILogger<GatewayOrchestrator> logger)
    {
        _frendzCredentialsRepository = frendzCredentialsRepository;
        _sigiloPayCredentialsRepository = sigiloPayCredentialsRepository;
        _wintechCredentialsRepository = wintechCredentialsRepository;
        _frendzServiceFactory = frendzServiceFactory;
        _sigiloPayServiceFactory = sigiloPayServiceFactory;
        _wintechServiceFactory = wintechServiceFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IResult<TryCreatePixResponse>> TryCreatePixAsync(TryCreatePixRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paymentId = request.PaymentId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return Result.Create<TryCreatePixResponse>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                    .WithMessage("O ID do pagamento é obrigatório.")
                    .Build())
                .Build();
        }

        if (request.Amount <= 0m)
        {
            return Result.Create<TryCreatePixResponse>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.AmountInvalid)
                    .WithMessage("O valor deve ser maior que zero.")
                    .Build())
                .Build();
        }

        if (request.Credentials.Count == 0)
        {
            return Result.Create<TryCreatePixResponse>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.NoGatewayServicesAvailable)
                    .WithMessage("Não há credenciais de gateway disponíveis.")
                    .Build())
                .Build();
        }

        var credentials = request.Credentials.ToArray();
        Random.Shared.Shuffle(credentials);

        foreach (var reference in credentials)
        {
            if (string.IsNullOrWhiteSpace(reference.CredentialId))
                continue;

            if (_options.UseMockOrchestrator)
            {
                _logger.LogWarning(
                    "GatewayOrchestrator mock generated PIX for payment {PaymentId} (gateway {Gateway}, credential {CredentialId}, amount {Amount}).",
                    paymentId,
                    reference.Gateway,
                    reference.CredentialId,
                    request.Amount);

                var transactionId = $"mock-{paymentId}";
                return Result.Create<TryCreatePixResponse>()
                    .WithValue(new TryCreatePixResponse
                    {
                        TransactionId = transactionId,
                        PixCode = BuildMockPixCode(paymentId, request.Amount),
                        Gateway = reference.Gateway,
                        CredentialId = reference.CredentialId.Trim(),
                    })
                    .Build();
            }

            try
            {
                var service = await CreateServiceAsync(reference);
                if (service is null)
                    continue;

                var pix = await service.CreatePixAsync(new CreatePixRequest
                {
                    PaymentId = paymentId,
                    Amount = request.Amount,
                });

                return Result.Create<TryCreatePixResponse>()
                    .WithValue(new TryCreatePixResponse
                    {
                        TransactionId = pix.TransactionId,
                        PixCode = pix.PixCode,
                        Gateway = reference.Gateway,
                        CredentialId = reference.CredentialId.Trim(),
                    })
                    .Build();
            }
            catch (Exception)
            {
                // Tenta a próxima credencial disponível.
            }
        }

        return Result.Create<TryCreatePixResponse>()
            .WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.GatewayPixFailed)
                .WithMessage("Todas as tentativas de processamento pelo gateway falharam. Verifique as credenciais configuradas e tente novamente.")
                .Build())
            .Build();
    }

    private async Task<IGatewayService?> CreateServiceAsync(GatewayCredentialReference reference)
    {
        return reference.Gateway switch
        {
            PaymentGateway.Frendz => await TryCreateFrendzServiceAsync(reference.CredentialId),
            PaymentGateway.SigiloPay => await TryCreateSigiloPayServiceAsync(reference.CredentialId),
            PaymentGateway.Wintech => await TryCreateWintechServiceAsync(reference.CredentialId),
            _ => null,
        };
    }

    private async Task<IGatewayService?> TryCreateFrendzServiceAsync(string credentialId)
    {
        var credential = await _frendzCredentialsRepository.AsQueryable()
            .Where(x => x.Id == credentialId)
            .FirstOrDefaultAsync();
        if (credential is null || !credential.Enabled)
            return null;

        return await _frendzServiceFactory.CreateAsync(credential);
    }

    private async Task<IGatewayService?> TryCreateSigiloPayServiceAsync(string credentialId)
    {
        var credential = await _sigiloPayCredentialsRepository.AsQueryable()
            .Where(x => x.Id == credentialId)
            .FirstOrDefaultAsync();
        if (credential is null || !credential.Enabled)
            return null;

        return await _sigiloPayServiceFactory.CreateAsync(credential);
    }

    private async Task<IGatewayService?> TryCreateWintechServiceAsync(string credentialId)
    {
        var credential = await _wintechCredentialsRepository.AsQueryable()
            .Where(x => x.Id == credentialId)
            .FirstOrDefaultAsync();
        if (credential is null || !credential.Enabled)
            return null;

        return await _wintechServiceFactory.CreateAsync(credential);
    }

    private static string BuildMockPixCode(string paymentId, decimal amount)
    {
        var amountText = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        return $"00020126580014BR.GOV.BCB.PIX0136MOCK-{paymentId}52040000530398654{amountText.Length:D2}{amountText}5802BR5925NEXUS MOCK GATEWAY6009SAO PAULO62070503***6304MOCK";
    }
}
