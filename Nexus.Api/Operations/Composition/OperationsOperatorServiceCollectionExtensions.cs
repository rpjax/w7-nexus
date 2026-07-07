using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Application.Services;

namespace Nexus.Operations.Composition;

public static class OperationsOperatorServiceCollectionExtensions
{
    public static IServiceCollection AddNexusOperationsOperator(this IServiceCollection services)
    {
        services.AddScoped<IOperatorAccessPolicy, OperatorAccessPolicy>();
        services.AddScoped<IOperatorOperationSearchService, OperatorOperationSearchService>();
        services.AddScoped<IOperator, Operator>();

        return services;
    }
}
