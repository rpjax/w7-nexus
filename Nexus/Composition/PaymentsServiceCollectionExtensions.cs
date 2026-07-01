using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.Application.Services;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Services;
using Nexus.Payments.Infrastructure.Notifications;
using Nexus.Payments.Infrastructure.Persistance;
using Nexus.Payments.Presentation;

namespace Nexus.Composition;

public static class PaymentsServiceCollectionExtensions
{
    public static IServiceCollection AddNexusPayments(this IServiceCollection services)
    {
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPaymentSplitCalculationService, PaymentSplitCalculationService>();
        services.AddScoped<IPaymentRepository, MongoPaymentRepository>();
        services.AddScoped<IPaymentDetailsEnrichmentService, PaymentDetailsEnrichmentService>();
        services.AddScoped<IGatewayPaymentWebhookService, GatewayPaymentWebhookService>();
        services.AddScoped<IPaymentNotifier, SignalRPaymentNotifier>();
        services.AddScoped<IAccountIdValidator, AccountIdValidator>();

        return services;
    }
}
