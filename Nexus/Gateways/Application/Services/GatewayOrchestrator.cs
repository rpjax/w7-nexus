using Aidan.Core.Errors;
using Nexus.Gateways.Wintech.Application.Services.Contracts;
using Nexus.Gateways.SigiloPay.Application.Services.Contracts;
using Nexus.Gateways.Frendz.Application.Services.Contracts;
using Nexus.Gateways.Application.Services.Contracts;
using Nexus.Payments.Application.Services.Contracts;
using Nexus.Operations.Application.Services.Contracts;
using Aidan.Core.Linq;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Payments.Application.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Services;
using Nexus.Gateways.Wintech.Application.Services;
using Nexus.Gateways.Application.Services;
using Nexus.Gateways.Frendz.Application.Services;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Services;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Services;
using Nexus.Payments.Errors;

namespace Nexus.Gateways.Application.Services;

public sealed class GatewayOrchestrator : IGatewayOrchestrator
{
    private IOperationRepository _operationRepository { get; }
    private ITeamRepository _teamRepository { get; }
    private IPaymentService _paymentService { get; }
    private IPaymentRepository _paymentRepository { get; }
    private IFrendzApiCredentialsRepository _frendzApiCredentialsRepository { get; }
    private IFrendzGatewayPixServiceFactory _frendzGatewayPixServiceFactory { get; }
    private ISigiloPayApiCredentialsRepository _sigiloPayApiCredentialsRepository { get; }
    private ISigiloPayGatewayPixServiceFactory _sigiloPayGatewayPixServiceFactory { get; }
    private IWintechApiCredentialsRepository _wintechApiCredentialsRepository { get; }
    private IWintechGatewayPixServiceFactory _wintechGatewayPixServiceFactory { get; }
    private IGatewayCredentialsGroupRepository _gatewayCredentialsGroupRepository { get; }

    public GatewayOrchestrator(
        IOperationRepository operationRepository,
        ITeamRepository teamRepository,
        IPaymentService paymentService,
        IPaymentRepository paymentRepository,
        IFrendzApiCredentialsRepository frendzApiCredentialsRepository,
        IFrendzGatewayPixServiceFactory frendzGatewayPixServiceFactory,
        ISigiloPayApiCredentialsRepository sigiloPayApiCredentialsRepository,
        ISigiloPayGatewayPixServiceFactory sigiloPayGatewayPixServiceFactory,
        IWintechApiCredentialsRepository wintechApiCredentialsRepository,
        IWintechGatewayPixServiceFactory wintechGatewayPixServiceFactory,
        IGatewayCredentialsGroupRepository gatewayCredentialsGroupRepository)
    {
        _operationRepository = operationRepository;
        _teamRepository = teamRepository;
        _paymentService = paymentService;
        _paymentRepository = paymentRepository;
        _frendzApiCredentialsRepository = frendzApiCredentialsRepository;
        _frendzGatewayPixServiceFactory = frendzGatewayPixServiceFactory;
        _sigiloPayApiCredentialsRepository = sigiloPayApiCredentialsRepository;
        _sigiloPayGatewayPixServiceFactory = sigiloPayGatewayPixServiceFactory;
        _wintechApiCredentialsRepository = wintechApiCredentialsRepository;
        _wintechGatewayPixServiceFactory = wintechGatewayPixServiceFactory;
        _gatewayCredentialsGroupRepository = gatewayCredentialsGroupRepository;
    }

    public async Task<IResult<GatewayPix>> CreateGatewayPixAsync(CreateGatewayPixRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var operationId = request.OperationId?.Trim();
        if (string.IsNullOrWhiteSpace(operationId))
            return Result.Create<GatewayPix>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperationIdInvalid)
                    .WithMessage("Operation ID is required")
                    .Build())
                .Build();

        var operatorAccountId = request.OperatorAccountId?.Trim();
        if (string.IsNullOrWhiteSpace(operatorAccountId))
            return Result.Create<GatewayPix>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperatorRequired)
                    .WithMessage("Operator account ID is required")
                    .Build())
                .Build();

        if (request.Amount <= 0m)
            return Result.Create<GatewayPix>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.AmountInvalid)
                    .WithMessage("Amount must be greater than zero")
                    .Build())
                .Build();

        var operation = _operationRepository.AsQueryable()
            .FirstOrDefault(x => x.Id == operationId);

        if (operation is null)
            return Result.Create<GatewayPix>()
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
            return Result.Create<GatewayPix>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.TeamNotFound)
                    .WithMessage($"No team was found for operator '{operatorAccountId}' in operation '{operationId}'")
                    .Build())
                .Build();

        var providers = await GetGatewayProvidersAsync(team);

        if (providers.Length == 0)
        {
            return Result.Create<GatewayPix>()
                .WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.NoGatewayServicesAvailable)
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
            return new ResultBuilder<GatewayPix>()
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
                var gatewayPixRequest = new CreateGatewayPixRequest
                {
                    PaymentId = payment.Id,
                    OperationId = operationId,
                    OperatorAccountId = operatorAccountId,
                    StrawManAccountId = provider.StrawManId,
                    Amount = request.Amount
                };

                var gatewayPix = await provider.Service.CreateGatewayPixAsync(gatewayPixRequest);

                var transactionId = gatewayPix.Id;
                var bindGateway = payment.BindToGateway(provider.Gateway, transactionId);
                if (bindGateway.IsFailure)
                {
                    await _paymentService.DeletePaymentAsync(payment.Id);
                    return Result.Create<GatewayPix>()
                        .WithErrors(bindGateway.Errors)
                        .Build();
                }

                if (!string.IsNullOrWhiteSpace(provider.StrawManId))
                {
                    var bindStrawMan = payment.BindToStrawMan(provider.StrawManId);
                    if (bindStrawMan.IsFailure)
                    {
                        await _paymentService.DeletePaymentAsync(payment.Id);
                        return Result.Create<GatewayPix>()
                            .WithErrors(bindStrawMan.Errors)
                            .Build();
                    }
                }

                await _paymentRepository.UpdateAsync(payment);
                return Result.Create<GatewayPix>()
                    .WithValue(gatewayPix)
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
        return Result.Create<GatewayPix>()
            .WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.GatewayPixFailed)
                .WithMessage("All gateway attempts failed.")
                .Build())
            .WithErrors(exceptionErrors)
            .Build();
    }

    private async Task<GatewayServiceProvider[]> GetGatewayProvidersAsync(Team team)
    {
        var allowedCredentialIds = await ResolveAllowedCredentialIdsAsync(team);
        var frendzProviders = await GetFrendzGatewayProvidersAsync(team, allowedCredentialIds);
        var sigiloPayProviders = await GetSigiloPayGatewayProvidersAsync(team, allowedCredentialIds);
        var wintechProviders = await GetWintechGatewayProvidersAsync(team, allowedCredentialIds);
        var merged = frendzProviders.Concat(sigiloPayProviders).Concat(wintechProviders).ToArray();
        Random.Shared.Shuffle(merged);
        return merged;
    }

    private async Task<string[]> ResolveAllowedCredentialIdsAsync(Team team)
    {
        return team.GatewaySelectionStrategy switch
        {
            GatewaySelectionStrategy.Manual => team.GatewayCredentialsIds.ToArray(),
            GatewaySelectionStrategy.PerGroup => await ResolveGroupCredentialIdsAsync(team),
            _ => Array.Empty<string>()
        };
    }

    private async Task<string[]> ResolveGroupCredentialIdsAsync(Team team)
    {
        var groupIds = team.GatewayCredentialsGroupIds.ToArray();
        if (groupIds.Length == 0)
            return Array.Empty<string>();

        var groups = await MaterializeAsync(
            _gatewayCredentialsGroupRepository.AsQueryable()
                .Where(g => groupIds.Contains(g.Id)));

        return groups
            .SelectMany(g => g.GatewayCredentialsIds)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static async Task<T[]> MaterializeAsync<T>(IAsyncQueryable<T> query)
    {
        try
        {
            return await query.ToArrayAsync();
        }
        catch (ArgumentException)
        {
            return query.AsEnumerable().ToArray();
        }
    }

    private async Task<GatewayServiceProvider[]> GetFrendzGatewayProvidersAsync(
        Team team,
        string[] allowedCredentialIds)
    {
        var strawmanIds = team.StrawManIds.ToArray();

        var query = _frendzApiCredentialsRepository.AsQueryable().Where(x => x.Enabled);
        query = team.GatewaySelectionStrategy is GatewaySelectionStrategy.Manual or GatewaySelectionStrategy.PerGroup
            ? query.Where(x => allowedCredentialIds.Contains(x.Id))
            : query.Where(x => x.StrawManId == null || strawmanIds.Contains(x.StrawManId));

        var credentials = await MaterializeAsync(query);

        var providers = new List<GatewayServiceProvider>();

        foreach (var credential in credentials)
        {
            var service = _frendzGatewayPixServiceFactory.Create(credential);
            var provider = new GatewayServiceProvider(
                gateway: PaymentGateway.Frendz,
                strawManId: credential.StrawManId,
                service: service);
            providers.Add(provider);
        }

        return providers.ToArray();
    }

    private async Task<GatewayServiceProvider[]> GetSigiloPayGatewayProvidersAsync(
        Team team,
        string[] allowedCredentialIds)
    {
        var strawmanIds = team.StrawManIds.ToArray();

        var query = _sigiloPayApiCredentialsRepository.AsQueryable().Where(x => x.Enabled);
        query = team.GatewaySelectionStrategy is GatewaySelectionStrategy.Manual or GatewaySelectionStrategy.PerGroup
            ? query.Where(x => allowedCredentialIds.Contains(x.Id))
            : query.Where(x => x.StrawManId == null || strawmanIds.Contains(x.StrawManId));

        var credentials = await MaterializeAsync(query);

        var providers = new List<GatewayServiceProvider>();

        foreach (var credential in credentials)
        {
            var service = _sigiloPayGatewayPixServiceFactory.Create(credential);
            var provider = new GatewayServiceProvider(
                gateway: PaymentGateway.SigiloPay,
                strawManId: credential.StrawManId,
                service: service);
            providers.Add(provider);
        }

        return providers.ToArray();
    }

    private async Task<GatewayServiceProvider[]> GetWintechGatewayProvidersAsync(
        Team team,
        string[] allowedCredentialIds)
    {
        var strawmanIds = team.StrawManIds.ToArray();

        var query = _wintechApiCredentialsRepository.AsQueryable().Where(x => x.Enabled);
        query = team.GatewaySelectionStrategy is GatewaySelectionStrategy.Manual or GatewaySelectionStrategy.PerGroup
            ? query.Where(x => allowedCredentialIds.Contains(x.Id))
            : query.Where(x => x.StrawManId == null || strawmanIds.Contains(x.StrawManId));

        var credentials = await MaterializeAsync(query);

        var providers = new List<GatewayServiceProvider>();

        foreach (var credential in credentials)
        {
            var service = _wintechGatewayPixServiceFactory.Create(credential);
            var provider = new GatewayServiceProvider(
                gateway: PaymentGateway.Wintech,
                strawManId: credential.StrawManId,
                service: service);
            providers.Add(provider);
        }

        return providers.ToArray();
    }
}
