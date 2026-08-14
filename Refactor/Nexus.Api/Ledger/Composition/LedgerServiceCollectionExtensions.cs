using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Authenticated.Queries;
using Refactor.Nexus.Api.Ledger.Infrastructure.Mandates;
using Refactor.Nexus.Api.Ledger.Infrastructure.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Ledger;

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
        services.AddScoped<IRevealClaimUseCase, RevealClaimHandler>();
        services.AddScoped<IGetMyStatementUseCase, GetMyStatementHandler>();
        services.AddScoped<IMarkAccountLostUseCase, MarkAccountLostHandler>();
        services.AddScoped<IReconcileAccountUseCase, ReconcileAccountHandler>();
        services.AddScoped<IReverseChargeUseCase, ReverseChargeHandler>();
        services.AddScoped<IListExposureUseCase, ListExposureHandler>();
        services.AddScoped<IArchiveClaimUseCase, ArchiveClaimHandler>();
        services.AddScoped<ILedgerClaimObservationPort, LedgerClaimObservationAdapter>();
        return services;
    }
}
