using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Application.Services;
using Nexus.Operations.Composition;
using Nexus.Operations.Infrastructure.Persistance;

namespace Nexus.Composition;

public static class OperationsServiceCollectionExtensions
{
    public static IServiceCollection AddNexusOperations(this IServiceCollection services)
    {
        services.AddScoped<IOperationRepository, MongoOperationRepository>();
        services.AddScoped<IOperationService, OperationService>();
        services.AddScoped<ITeamRepository, MongoTeamRepository>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddNexusOperationsAdministrator();

        return services;
    }
}
