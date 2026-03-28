using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Charges.Application;
using Nexus.Charges.Application.Models;
using Nexus.Frendz.Application;
using Nexus.SigiloPay.Application;
using Nexus.Wintech.Application;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application;
using Nexus.Payments.Application.Models;
using Nexus.Payments.ErrorCodes;

namespace Nexus.Charges.Infrastructure;

public sealed class ChargeOrchestrator : IChargeOrchestrator
{
    private IOperationRepository _operationRepository { get; }
    private IPaymentService _paymentService { get; }
    private IPaymentRepository _paymentRepository { get; }
    private IFrendzApiCredentialsRepository _frendzApiCredentialsRepository { get; }
    private IFrendzChargeServiceFactory _frendzChargeServiceFactory { get; }
    private ISigiloPayApiCredentialsRepository _sigiloPayApiCredentialsRepository { get; }
    private ISigiloPayChargeServiceFactory _sigiloPayChargeServiceFactory { get; }
    private IWintechApiCredentialsRepository _wintechApiCredentialsRepository { get; }
    private IWintechChargeServiceFactory _wintechChargeServiceFactory { get; }

    public ChargeOrchestrator(
        IOperationRepository operationRepository,
        IPaymentService paymentService,
        IPaymentRepository paymentRepository,
        IFrendzApiCredentialsRepository frendzApiCredentialsRepository,
        IFrendzChargeServiceFactory frendzChargeServiceFactory,
        ISigiloPayApiCredentialsRepository sigiloPayApiCredentialsRepository,
        ISigiloPayChargeServiceFactory sigiloPayChargeServiceFactory,
        IWintechApiCredentialsRepository wintechApiCredentialsRepository,
        IWintechChargeServiceFactory wintechChargeServiceFactory)
    {
        _operationRepository = operationRepository;
        _paymentService = paymentService;
        _paymentRepository = paymentRepository;
        _frendzApiCredentialsRepository = frendzApiCredentialsRepository;
        _frendzChargeServiceFactory = frendzChargeServiceFactory;
        _sigiloPayApiCredentialsRepository = sigiloPayApiCredentialsRepository;
        _sigiloPayChargeServiceFactory = sigiloPayChargeServiceFactory;
        _wintechApiCredentialsRepository = wintechApiCredentialsRepository;
        _wintechChargeServiceFactory = wintechChargeServiceFactory;
    }

    public async Task<IResult<PixCharge>> CreatePixChargeAsync(CreatePixChargeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var operationId = request.OperationId?.Trim();
        if (string.IsNullOrWhiteSpace(operationId))
            return Result.Create<PixCharge>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperationIdInvalid)
                    .WithMessage("Operation ID is required")
                    .Build())
                .Build();

        if (request.Amount <= 0m)
            return Result.Create<PixCharge>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.AmountInvalid)
                    .WithMessage("Amount must be greater than zero")
                    .Build())
                .Build();

        var operation = _operationRepository.AsQueryable()
            .FirstOrDefault(x => x.Id == operationId);

        if (operation is null)
            return Result.Create<PixCharge>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperationNotFound)
                    .WithMessage($"Operation '{operationId}' was not found")
                    .Build())
                .Build();

        var providers = await GetChargeProvidersAsync(operation);

        if (providers.Length == 0)
        {
            return Result.Create<PixCharge>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.NoChargeServicesAvailable)
                    .WithMessage("No gateway credentials are available for this operation.")
                    .Build())
                .Build();
        }

        var paymentId = string.IsNullOrWhiteSpace(request.PaymentId)
            ? Guid.NewGuid().ToString("N")
            : request.PaymentId.Trim();

        // Straw man só é conhecido após uma tentativa de cobrança bem-sucedida (via provider).
        var createPaymentRequest = new CreatePaymentRequest
        {
            ExplicitPaymentId = paymentId,
            OperationId = operationId,
            OperatorAccountId = request.OperatorAccountId,
            StrawManAccountId = null,
            Gateway = PaymentGateway.Frendz,
            Amount = request.Amount,
            GatewayPaymentId = paymentId,
        };

        var createPaymentResult = await _paymentService.CreatePaymentAsync(createPaymentRequest);
        if (createPaymentResult.IsFailure)
        {
            return new ResultBuilder<PixCharge>()
                .WithErrors(createPaymentResult.Errors)
                .Build();
        }

        var payment = createPaymentResult.Value
            ?? throw new InvalidOperationException("Payment creation succeeded without a value.");

        var exceptions = new List<Exception>();
        foreach (var provider in providers)
        {
            try
            {
                var chargeRequest = new CreatePixChargeRequest
                {
                    PaymentId = payment.Id,
                    OperationId = operationId,
                    OperatorAccountId = request.OperatorAccountId,
                    StrawManAccountId = provider.StrawManId,
                    Amount = request.Amount
                };

                // create the charge
                var pixCharge = await provider.Service.CreatePixChargeAsync(chargeRequest);

                // ID da transação no gateway (PK usada nos webhooks/postbacks para correlacionar:
                // SigiloPay/Wintech → transaction.id; Frendz → transaction_hash). Origem: parsers da resposta de criação PIX.
                var transactionId = pixCharge.Id;
                var bindGateway = payment.BindToGateway(provider.Gateway, transactionId);
                if (bindGateway.IsFailure)
                {
                    await _paymentService.DeletePaymentAsync(payment.Id);
                    return Result.Create<PixCharge>()
                        .WithErrors(bindGateway.Errors)
                        .Build();
                }

                // bind to strawman
                if (!string.IsNullOrWhiteSpace(provider.StrawManId))
                {
                    var bindStrawMan = payment.BindToStrawMan(provider.StrawManId);
                    if (bindStrawMan.IsFailure)
                    {
                        await _paymentService.DeletePaymentAsync(payment.Id);
                        return Result.Create<PixCharge>()
                            .WithErrors(bindStrawMan.Errors)
                            .Build();
                    }
                }
                
                // updates the payments
                await _paymentRepository.UpdateAsync(payment);
                return Result.Create<PixCharge>()
                    .WithValue(pixCharge)
                    .Build();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        var exceptionErrors = exceptions
            .Select(Error.FromException)
            .ToArray();

        await _paymentService.DeletePaymentAsync(payment.Id);
        return Result.Create<PixCharge>()
            .WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.ChargeGatewayFailed)
                .WithMessage("All gateway attempts failed.")
                .Build())
            .WithErrors(exceptionErrors)
            .Build();
    }

    /// <summary>
    /// Reúne todos os provedores de cobrança elegíveis (por gateway) e embaralha a ordem de tentativa.
    /// </summary>
    private async Task<ChargeServiceProvider[]> GetChargeProvidersAsync(Operation operation)
    {
        var frendzProviders = await GetFrendzChargeProvidersAsync(operation);
        var sigiloPayProviders = await GetSigiloPayChargeProvidersAsync(operation);
        var wintechProviders = await GetWintechChargeProvidersAsync(operation);
        var merged = frendzProviders.Concat(sigiloPayProviders).Concat(wintechProviders).ToArray();
        Random.Shared.Shuffle(merged);
        return merged;
    }

    private async Task<ChargeServiceProvider[]> GetFrendzChargeProvidersAsync(Operation operation)
    {
        var strawmanIds = operation.StrawManIds.ToArray();
        var manualCredentialIds = operation.ChargeCredentialsIds.ToArray();

        var credentials = await _frendzApiCredentialsRepository.AsQueryable()
            .Where(x => x.Enabled && (
                operation.ManuallySetChargeCredentials
                    ? manualCredentialIds.Contains(x.Id)
                    : (x.StrawManId == null || strawmanIds.Contains(x.StrawManId))))
            .ToArrayAsync();

        var providers = new List<ChargeServiceProvider>();

        foreach (var credential in credentials)
        {
            var service = _frendzChargeServiceFactory.Create(credential);
            var provider = new ChargeServiceProvider(
                gateway: PaymentGateway.Frendz,
                strawManId: credential.StrawManId,
                service: service);
            providers.Add(provider);
        }

        return providers.ToArray();
    }

    private async Task<ChargeServiceProvider[]> GetSigiloPayChargeProvidersAsync(Operation operation)
    {
        var strawmanIds = operation.StrawManIds.ToArray();
        var manualCredentialIds = operation.ChargeCredentialsIds.ToArray();

        var credentials = await _sigiloPayApiCredentialsRepository.AsQueryable()
            .Where(x => x.Enabled && (
                operation.ManuallySetChargeCredentials
                    ? manualCredentialIds.Contains(x.Id)
                    : (x.StrawManId == null || strawmanIds.Contains(x.StrawManId))))
            .ToArrayAsync();

        var providers = new List<ChargeServiceProvider>();

        foreach (var credential in credentials)
        {
            var service = _sigiloPayChargeServiceFactory.Create(credential);
            var provider = new ChargeServiceProvider(
                gateway: PaymentGateway.SigiloPay,
                strawManId: credential.StrawManId,
                service: service);
            providers.Add(provider);
        }

        return providers.ToArray();
    }

    private async Task<ChargeServiceProvider[]> GetWintechChargeProvidersAsync(Operation operation)
    {
        var strawmanIds = operation.StrawManIds.ToArray();
        var manualCredentialIds = operation.ChargeCredentialsIds.ToArray();

        var credentials = await _wintechApiCredentialsRepository.AsQueryable()
            .Where(x => x.Enabled && (
                operation.ManuallySetChargeCredentials
                    ? manualCredentialIds.Contains(x.Id)
                    : (x.StrawManId == null || strawmanIds.Contains(x.StrawManId))))
            .ToArrayAsync();

        var providers = new List<ChargeServiceProvider>();

        foreach (var credential in credentials)
        {
            var service = _wintechChargeServiceFactory.Create(credential);
            var provider = new ChargeServiceProvider(
                gateway: PaymentGateway.Wintech,
                strawManId: credential.StrawManId,
                service: service);
            providers.Add(provider);
        }

        return providers.ToArray();
    }
}
