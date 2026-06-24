using Aidan.Core.Linq;
using Aidan.Core.Linq.Extensions;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.Frendz.Application.Services;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Services;
using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Gateways.Wintech.Application.Services;
using Nexus.Operations.Aggregates;
using Nexus.Payments.Aggregates;

namespace Nexus.Gateways.Application.Services;

public sealed class GatewayCredentialProviderResolver
{
    private IFrendzApiCredentialsRepository _frendzApiCredentialsRepository { get; }
    private IFrendzGatewayPixServiceFactory _frendzGatewayPixServiceFactory { get; }
    private ISigiloPayApiCredentialsRepository _sigiloPayApiCredentialsRepository { get; }
    private ISigiloPayGatewayPixServiceFactory _sigiloPayGatewayPixServiceFactory { get; }
    private IWintechApiCredentialsRepository _wintechApiCredentialsRepository { get; }
    private IWintechGatewayPixServiceFactory _wintechGatewayPixServiceFactory { get; }
    private IGatewayCredentialsGroupRepository _gatewayCredentialsGroupRepository { get; }

    public GatewayCredentialProviderResolver(
        IFrendzApiCredentialsRepository frendzApiCredentialsRepository,
        IFrendzGatewayPixServiceFactory frendzGatewayPixServiceFactory,
        ISigiloPayApiCredentialsRepository sigiloPayApiCredentialsRepository,
        ISigiloPayGatewayPixServiceFactory sigiloPayGatewayPixServiceFactory,
        IWintechApiCredentialsRepository wintechApiCredentialsRepository,
        IWintechGatewayPixServiceFactory wintechGatewayPixServiceFactory,
        IGatewayCredentialsGroupRepository gatewayCredentialsGroupRepository)
    {
        _frendzApiCredentialsRepository = frendzApiCredentialsRepository;
        _frendzGatewayPixServiceFactory = frendzGatewayPixServiceFactory;
        _sigiloPayApiCredentialsRepository = sigiloPayApiCredentialsRepository;
        _sigiloPayGatewayPixServiceFactory = sigiloPayGatewayPixServiceFactory;
        _wintechApiCredentialsRepository = wintechApiCredentialsRepository;
        _wintechGatewayPixServiceFactory = wintechGatewayPixServiceFactory;
        _gatewayCredentialsGroupRepository = gatewayCredentialsGroupRepository;
    }

    public async Task<GatewayServiceProvider[]> ResolveProvidersAsync(IGatewayCredentialScope scope)
    {
        var allowedCredentialIds = await ResolveAllowedCredentialIdsAsync(scope);
        var frendzProviders = await GetFrendzGatewayProvidersAsync(scope, allowedCredentialIds);
        var sigiloPayProviders = await GetSigiloPayGatewayProvidersAsync(scope, allowedCredentialIds);
        var wintechProviders = await GetWintechGatewayProvidersAsync(scope, allowedCredentialIds);
        var merged = frendzProviders
            .Concat(sigiloPayProviders)
            .Concat(wintechProviders)
            .Where(p => !string.IsNullOrWhiteSpace(p.StrawManId))
            .ToArray();
        Random.Shared.Shuffle(merged);
        return merged;
    }

    private async Task<string[]> ResolveAllowedCredentialIdsAsync(IGatewayCredentialScope scope)
    {
        return scope.GatewaySelectionStrategy switch
        {
            GatewaySelectionStrategy.Manual => scope.GatewayCredentialsIds.ToArray(),
            GatewaySelectionStrategy.PerGroup => await ResolveGroupCredentialIdsAsync(scope),
            _ => Array.Empty<string>()
        };
    }

    private async Task<string[]> ResolveGroupCredentialIdsAsync(IGatewayCredentialScope scope)
    {
        var groupIds = scope.GatewayCredentialsGroupIds.ToArray();
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
        IGatewayCredentialScope scope,
        string[] allowedCredentialIds)
    {
        if (scope.GatewaySelectionStrategy is GatewaySelectionStrategy.PerStrawman &&
            scope.StrawManIds.Count == 0)
        {
            return Array.Empty<GatewayServiceProvider>();
        }

        var strawmanIds = scope.StrawManIds.ToArray();

        var query = _frendzApiCredentialsRepository.AsQueryable().Where(x => x.Enabled);
        query = scope.GatewaySelectionStrategy switch
        {
            GatewaySelectionStrategy.Manual or GatewaySelectionStrategy.PerGroup =>
                query.Where(x => allowedCredentialIds.Contains(x.Id)),
            GatewaySelectionStrategy.PerStrawman =>
                query.Where(x => !string.IsNullOrWhiteSpace(x.StrawManId) && strawmanIds.Contains(x.StrawManId)),
            _ => query
        };

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
        IGatewayCredentialScope scope,
        string[] allowedCredentialIds)
    {
        if (scope.GatewaySelectionStrategy is GatewaySelectionStrategy.PerStrawman &&
            scope.StrawManIds.Count == 0)
        {
            return Array.Empty<GatewayServiceProvider>();
        }

        var strawmanIds = scope.StrawManIds.ToArray();

        var query = _sigiloPayApiCredentialsRepository.AsQueryable().Where(x => x.Enabled);
        query = scope.GatewaySelectionStrategy switch
        {
            GatewaySelectionStrategy.Manual or GatewaySelectionStrategy.PerGroup =>
                query.Where(x => allowedCredentialIds.Contains(x.Id)),
            GatewaySelectionStrategy.PerStrawman =>
                query.Where(x => !string.IsNullOrWhiteSpace(x.StrawManId) && strawmanIds.Contains(x.StrawManId)),
            _ => query
        };

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
        IGatewayCredentialScope scope,
        string[] allowedCredentialIds)
    {
        if (scope.GatewaySelectionStrategy is GatewaySelectionStrategy.PerStrawman &&
            scope.StrawManIds.Count == 0)
        {
            return Array.Empty<GatewayServiceProvider>();
        }

        var strawmanIds = scope.StrawManIds.ToArray();

        var query = _wintechApiCredentialsRepository.AsQueryable().Where(x => x.Enabled);
        query = scope.GatewaySelectionStrategy switch
        {
            GatewaySelectionStrategy.Manual or GatewaySelectionStrategy.PerGroup =>
                query.Where(x => allowedCredentialIds.Contains(x.Id)),
            GatewaySelectionStrategy.PerStrawman =>
                query.Where(x => !string.IsNullOrWhiteSpace(x.StrawManId) && strawmanIds.Contains(x.StrawManId)),
            _ => query
        };

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
