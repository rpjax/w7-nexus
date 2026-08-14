using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.Ledger.Infrastructure.Mandates;
using Refactor.Nexus.Api.Ledger.Infrastructure.Persistence;

namespace Refactor.Nexus.Api.Ledger.Composition;

public static class LedgerServiceCollectionExtensions
{
    public static IServiceCollection AddRefactorLedger(this IServiceCollection services)
    {
        services.AddScoped<ILedgerAccess, LedgerAccessAdapter>();
        services.AddScoped<MartenClaimRepository>();
        services.AddScoped<IClaimRepository>(sp => sp.GetRequiredService<MartenClaimRepository>());
        services.AddScoped<IHopRepository>(sp => sp.GetRequiredService<MartenClaimRepository>());
        services.AddScoped<ILedgerCommit>(sp => sp.GetRequiredService<MartenClaimRepository>());
        services.AddScoped<IMaterializationCommit, MaterializationCommitAdapter>();
        services.AddScoped<IMaterializeChargeUseCase, MaterializeChargeHandler>();
        services.AddScoped<IRegisterHopUseCase, RegisterHopHandler>();
        services.AddScoped<IRepassClaimsUseCase, RepassClaimsHandler>();
        services.AddScoped<IListClaimsUseCase, ListClaimsHandler>();
        services.AddScoped<IGetClaimUseCase, GetClaimHandler>();
        services.AddScoped<IListHopsUseCase, ListHopsHandler>();
        return services;
    }
}
