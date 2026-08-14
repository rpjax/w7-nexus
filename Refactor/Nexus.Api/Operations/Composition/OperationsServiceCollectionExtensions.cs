using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Operations;
using Refactor.Nexus.Api.Mandates.Infrastructure.Operations;
using Refactor.Nexus.Api.Operations.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Operations.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Operations.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.Operations.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.Operations.Application.UseCases.Edge.Queries;
using Refactor.Nexus.Api.Operations.Domain.Services;
using Refactor.Nexus.Api.Operations.Infrastructure.Mandates;
using Refactor.Nexus.Api.Operations.Infrastructure.Persistence;

namespace Refactor.Nexus.Api.Operations.Composition;

public static class OperationsServiceCollectionExtensions
{
    public static IServiceCollection AddRefactorOperations(this IServiceCollection services)
    {
        services.AddSingleton<IOperationActivityPolicy, OperationActivityPolicy>();

        services.AddScoped<IMandateCapabilityGate, MandateCapabilityGateAdapter>();
        services.AddScoped<IOperatorEligibility, OperatorEligibilityAdapter>();

        services.AddScoped<MartenOperationRepository>();
        services.AddScoped<IOperationRepository>(sp => sp.GetRequiredService<MartenOperationRepository>());
        services.AddScoped<IOperationReadRepository>(sp => sp.GetRequiredService<MartenOperationRepository>());

        services.AddScoped<PostgresScriptArtifactRepository>();
        services.AddScoped<IScriptArtifactRepository>(sp => sp.GetRequiredService<PostgresScriptArtifactRepository>());

        services.AddScoped<PostgresStoreObjectRepository>();
        services.AddScoped<IStoreObjectRepository>(sp => sp.GetRequiredService<PostgresStoreObjectRepository>());

        services.AddScoped<ICreateOperationUseCase, CreateOperationHandler>();
        services.AddScoped<ITransitionOperationUseCase, TransitionOperationHandler>();
        services.AddScoped<IConfigureManagementCutUseCase, ConfigureManagementCutHandler>();
        services.AddScoped<IAssignOperatorUseCase, AssignOperatorHandler>();
        services.AddScoped<IUnassignOperatorUseCase, UnassignOperatorHandler>();
        services.AddScoped<IRegisterScriptUseCase, RegisterScriptHandler>();
        services.AddScoped<IUpsertStoreObjectUseCase, UpsertStoreObjectHandler>();
        services.AddScoped<IDeleteStoreObjectUseCase, DeleteStoreObjectHandler>();
        services.AddScoped<IListOperationsUseCase, ListOperationsHandler>();
        services.AddScoped<IGetOperationUseCase, GetOperationHandler>();
        services.AddScoped<IListStoreObjectsUseCase, ListStoreObjectsHandler>();
        services.AddScoped<IResolveScriptUseCase, ResolveScriptHandler>();
        services.AddScoped<IGetStoreObjectUseCase, GetStoreObjectHandler>();

        services.AddScoped<IOperationsDatabaseInitializer, OperationsDatabaseInitializer>();

        return services;
    }
}
