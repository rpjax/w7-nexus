using Nexus.Operators.Application.Contracts;
using Nexus.Operators.Application.Services;

namespace Nexus.Operators.Composition;

public static class OperatorServiceCollectionExtensions
{
    public static IServiceCollection AddNexusOperator(this IServiceCollection services)
    {
        services.AddScoped<IOperatorAccessPolicy, OperatorAccessPolicy>();
        services.AddScoped<IOperatorOperationSearchService, OperatorOperationSearchService>();
        services.AddScoped<IOperatorPaymentSearchService, OperatorPaymentSearchService>();
        services.AddScoped<IOperator, Operator>();

        return services;
    }
}
