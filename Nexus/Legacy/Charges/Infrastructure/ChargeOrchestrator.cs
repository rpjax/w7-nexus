using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Legacy.Payments.Application.Models;
using Nexus.Legacy.Wintech.Application;
using Nexus.Legacy.SigiloPay.Application;
using Nexus.Legacy.Payments.Aggregates;
using Nexus.Legacy.Payments.Application;
using Nexus.Legacy.Payments.ErrorCodes;
using Nexus.Legacy.Frendz.Application;
using Nexus.Legacy.Charges.Application;
using Nexus.Legacy.Charges.Application.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application;

namespace Nexus.Legacy.Charges.Infrastructure;

public sealed class ChargeOrchestrator : IChargeOrchestrator
{
    private IOperationRepository _operationRepository { get; }
    private ITeamRepository _teamRepository { get; }
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
        ITeamRepository teamRepository,
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
        _teamRepository = teamRepository;
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

        var operatorAccountId = request.OperatorAccountId?.Trim();
        if (string.IsNullOrWhiteSpace(operatorAccountId))
            return Result.Create<PixCharge>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperatorRequired)
                    .WithMessage("Operator account ID is required")
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

        var team = _teamRepository.AsQueryable()
            .FirstOrDefault(t =>
                t.OperationId == operationId &&
                t.OperatorIds.Contains(operatorAccountId));

        if (team is null)
            return Result.Create<PixCharge>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.TeamNotFound)
                    .WithMessage($"No team was found for operator '{operatorAccountId}' in operation '{operationId}'")
                    .Build())
                .Build();

        var providers = await GetChargeProvidersAsync(team);

        if (providers.Length == 0)
        {
            return Result.Create<PixCharge>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.NoChargeServicesAvailable)
                    .WithMessage("No gateway credentials are available for this team.")
                    .Build())
                .Build();
        }

        var paymentId = string.IsNullOrWhiteSpace(request.PaymentId)
            ? Guid.NewGuid().ToString("N")
            : request.PaymentId.Trim();

        var createPaymentRequest = new CreatePaymentRequest
        {
            ExplicitPaymentId = paymentId,
            OperationId = operationId,
            OperatorAccountId = operatorAccountId,
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
                    OperatorAccountId = operatorAccountId,
                    StrawManAccountId = provider.StrawManId,
                    Amount = request.Amount
                };

                var pixCharge = await provider.Service.CreatePixChargeAsync(chargeRequest);

                var transactionId = pixCharge.Id;
                var bindGateway = payment.BindToGateway(provider.Gateway, transactionId);
                if (bindGateway.IsFailure)
                {
                    await _paymentService.DeletePaymentAsync(payment.Id);
                    return Result.Create<PixCharge>()
                        .WithErrors(bindGateway.Errors)
                        .Build();
                }

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

    private async Task<ChargeServiceProvider[]> GetChargeProvidersAsync(Team team)
    {
        var frendzProviders = await GetFrendzChargeProvidersAsync(team);
        var sigiloPayProviders = await GetSigiloPayChargeProvidersAsync(team);
        var wintechProviders = await GetWintechChargeProvidersAsync(team);
        var merged = frendzProviders.Concat(sigiloPayProviders).Concat(wintechProviders).ToArray();
        Random.Shared.Shuffle(merged);
        return merged;
    }

    private async Task<ChargeServiceProvider[]> GetFrendzChargeProvidersAsync(Team team)
    {
        var strawmanIds = team.StrawManIds.ToArray();
        var manualCredentialIds = team.GatewayCredentialsIds.ToArray();

        var credentials = await _frendzApiCredentialsRepository.AsQueryable()
            .Where(x => x.Enabled && (
                team.ManuallySetChargeCredentials
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

    private async Task<ChargeServiceProvider[]> GetSigiloPayChargeProvidersAsync(Team team)
    {
        var strawmanIds = team.StrawManIds.ToArray();
        var manualCredentialIds = team.GatewayCredentialsIds.ToArray();

        var credentials = await _sigiloPayApiCredentialsRepository.AsQueryable()
            .Where(x => x.Enabled && (
                team.ManuallySetChargeCredentials
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

    private async Task<ChargeServiceProvider[]> GetWintechChargeProvidersAsync(Team team)
    {
        var strawmanIds = team.StrawManIds.ToArray();
        var manualCredentialIds = team.GatewayCredentialsIds.ToArray();

        var credentials = await _wintechApiCredentialsRepository.AsQueryable()
            .Where(x => x.Enabled && (
                team.ManuallySetChargeCredentials
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
