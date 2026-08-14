using Refactor.Nexus.Api.Charging.Application.Ports.Out.Issuing;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Operations;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Charging.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.Charging.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.Charging.Application.UseCases.Authenticated.Commands;
using Refactor.Nexus.Api.Charging.Infrastructure.Issuing;
using Refactor.Nexus.Api.Charging.Infrastructure.Mandates;
using Refactor.Nexus.Api.Charging.Infrastructure.Operations;
using Refactor.Nexus.Api.Charging.Infrastructure.Persistence;

namespace Refactor.Nexus.Api.Charging.Composition;

public static class ChargingServiceCollectionExtensions
{
    public static IServiceCollection AddRefactorCharging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPaymentIssuer, NoOpPaymentIssuer>();
        services.AddScoped<IOperationChargingDirectory, OperationChargingDirectoryAdapter>();
        services.AddScoped<IChargingMandateSnapshot, ChargingMandateSnapshotAdapter>();

        services.AddScoped<IOperationEmissionSetRepository, PostgresOperationEmissionSetRepository>();
        services.AddScoped<IChargeRepository, MartenChargeRepository>();

        services.AddScoped<IBindEmissionRailUseCase, BindEmissionRailHandler>();
        services.AddScoped<IUnbindEmissionRailUseCase, UnbindEmissionRailHandler>();
        services.AddScoped<ICreateChargeUseCase, CreateChargeHandler>();
        services.AddScoped<ITransitionChargeUseCase, TransitionChargeHandler>();
        services.AddScoped<IMarkChargePaidUseCase, MarkChargePaidHandler>();
        services.AddScoped<IListEmissionRailsUseCase, ListEmissionRailsHandler>();
        services.AddScoped<IListOperationEmissionSetUseCase, ListOperationEmissionSetHandler>();
        services.AddScoped<IListChargesUseCase, ListChargesHandler>();
        services.AddScoped<IGetChargeUseCase, GetChargeHandler>();

        services.AddScoped<IChargingDatabaseInitializer, ChargingDatabaseInitializer>();

        return services;
    }
}
