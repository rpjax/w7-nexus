using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Services;

namespace Nexus.Payments.Composition;

public static class PaymentsOperatorServiceCollectionExtensions
{
    public static IServiceCollection AddNexusPaymentsOperator(this IServiceCollection services)
    {
        services.AddScoped<IOperatorAccessPolicy, OperatorAccessPolicy>();
        services.AddScoped<IOperatorPaymentSearchService, OperatorPaymentSearchService>();
        services.AddScoped<IOperator, Operator>();

        return services;
    }
}
