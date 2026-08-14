using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Operations;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.CloseAgencyDeal;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.GrantCapability;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.GrantPreset;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.RemoveShareholderStake;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.RevokeCapability;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.RevokePreset;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.UpsertAgencyDeal;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.UpsertShareholderStake;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Queries.GetMemberMandate;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Queries.ListAgencyDeals;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Queries.ListShareholders;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Authenticated.Queries.GetMyCarteira;
using Refactor.Nexus.Api.Mandates.Infrastructure.Identity;
using Refactor.Nexus.Api.Mandates.Infrastructure.Operations;
using Refactor.Nexus.Api.Mandates.Infrastructure.Persistence;

namespace Refactor.Nexus.Api.Mandates.Composition;

public static class MandatesServiceCollectionExtensions
{
    public static IServiceCollection AddRefactorMandates(this IServiceCollection services)
    {
        services.AddScoped<IAccountDirectory, AccountDirectoryAdapter>();
        services.AddScoped<IMandateAccessPolicy, MandateAccessPolicy>();
        services.AddScoped<OperationDirectoryAdapter>();
        services.AddScoped<IOperationDirectory>(sp => sp.GetRequiredService<OperationDirectoryAdapter>());
        services.AddScoped<IOperationAssignmentProbe>(sp => sp.GetRequiredService<OperationDirectoryAdapter>());

        services.AddScoped<MartenMandateRepositories>();
        services.AddScoped<IMemberMandateRepository>(sp => sp.GetRequiredService<MartenMandateRepositories>());
        services.AddScoped<IMemberMandateReadRepository>(sp => sp.GetRequiredService<MartenMandateRepositories>());

        services.AddScoped<IAgencyDealRepository>(sp => sp.GetRequiredService<MartenMandateRepositories>());
        services.AddScoped<IAgencyDealReadRepository>(sp => sp.GetRequiredService<MartenMandateRepositories>());

        services.AddScoped<IShareholderStakeRepository>(sp => sp.GetRequiredService<MartenMandateRepositories>());
        services.AddScoped<IShareholderStakeReadRepository>(sp => sp.GetRequiredService<MartenMandateRepositories>());

        services.AddScoped<IGrantPresetUseCase, GrantPresetHandler>();
        services.AddScoped<IRevokePresetUseCase, RevokePresetHandler>();
        services.AddScoped<IGrantCapabilityUseCase, GrantCapabilityHandler>();
        services.AddScoped<IRevokeCapabilityUseCase, RevokeCapabilityHandler>();
        services.AddScoped<IUpsertAgencyDealUseCase, UpsertAgencyDealHandler>();
        services.AddScoped<ICloseAgencyDealUseCase, CloseAgencyDealHandler>();
        services.AddScoped<IUpsertShareholderStakeUseCase, UpsertShareholderStakeHandler>();
        services.AddScoped<IRemoveShareholderStakeUseCase, RemoveShareholderStakeHandler>();
        services.AddScoped<IGetMemberMandateUseCase, GetMemberMandateHandler>();
        services.AddScoped<IListAgencyDealsUseCase, ListAgencyDealsHandler>();
        services.AddScoped<IListShareholdersUseCase, ListShareholdersHandler>();
        services.AddScoped<IGetMyCarteiraUseCase, GetMyCarteiraHandler>();

        services.AddScoped<IMandatesDatabaseInitializer, MandatesDatabaseInitializer>();

        return services;
    }
}
