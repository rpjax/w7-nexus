using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.WorldAccounts.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.WorldAccounts.Infrastructure.Mandates;
using Refactor.Nexus.Api.WorldAccounts.Infrastructure.Persistence;

namespace Refactor.Nexus.Api.WorldAccounts.Composition;

public static class WorldAccountsServiceCollectionExtensions
{
    public static IServiceCollection AddRefactorWorldAccounts(this IServiceCollection services)
    {
        services.AddScoped<IWorldAccountAccess, WorldAccountAccessAdapter>();
        services.AddScoped<MartenWorldAccountRepository>();
        services.AddScoped<IWorldAccountRepository>(sp => sp.GetRequiredService<MartenWorldAccountRepository>());

        services.AddScoped<IOpenWorldAccountUseCase, OpenWorldAccountHandler>();
        services.AddScoped<ILabelWorldAccountUseCase, LabelWorldAccountHandler>();
        services.AddScoped<IConfigureWorldAccountUseCase, ConfigureWorldAccountHandler>();
        services.AddScoped<IRecordWorldAccountObservationUseCase, RecordWorldAccountObservationHandler>();
        services.AddScoped<IListWorldAccountsUseCase, ListWorldAccountsHandler>();
        services.AddScoped<IGetWorldAccountUseCase, GetWorldAccountHandler>();
        services.AddScoped<IListWorldAccountTransactionsUseCase, ListWorldAccountTransactionsHandler>();

        return services;
    }

    public static async Task InitializeWorldAccountsAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MartenWorldAccountRepository>().BackfillLegacyRailsAsync();
    }
}
