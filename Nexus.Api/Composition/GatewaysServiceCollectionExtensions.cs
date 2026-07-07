using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Options;
using Nexus.Gateways.Application.Services;
using Nexus.Gateways.Composition;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.Frendz.Application.Services;
using Nexus.Gateways.Frendz.Infrastructure.Http;
using Nexus.Gateways.Frendz.Infrastructure.Persistance;
using Nexus.Gateways.Infrastructure.Persistance;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Services;
using Nexus.Gateways.SigiloPay.Infrastructure.Http;
using Nexus.Gateways.SigiloPay.Infrastructure.Persistance;
using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Gateways.Wintech.Application.Services;
using Nexus.Gateways.Wintech.Infrastructure.Http;
using Nexus.Gateways.Wintech.Infrastructure.Persistance;

namespace Nexus.Composition;

public static class GatewaysServiceCollectionExtensions
{
    public static IServiceCollection AddNexusGateways(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GatewaysOptions>(configuration.GetSection(GatewaysOptions.SectionName));

        services.AddScoped<IFrendzApiCredentialsRepository, MongoFrendzApiCredentialsRepository>();
        services.AddScoped<ISigiloPayApiCredentialsRepository, MongoSigiloPayApiCredentialsRepository>();
        services.AddScoped<IWintechApiCredentialsRepository, MongoWintechApiCredentialsRepository>();
        services.AddScoped<IFrendzServiceFactory, FrendzServiceFactory>();
        services.AddScoped<ISigiloPayServiceFactory, SigiloPayServiceFactory>();
        services.AddScoped<IWintechServiceFactory, WintechServiceFactory>();
        services.AddScoped<IGatewayCredentialsGroupRepository, MongoGatewayCredentialsGroupRepository>();
        services.AddScoped<IGatewayCredentialsGroupService, GatewayCredentialsGroupService>();
        services.AddScoped<IGatewayCredentialsIdValidator, GatewayCredentialsIdValidator>();
        services.AddScoped<IGatewayOrchestrator, GatewayOrchestrator>();

        services.AddScoped<IFrendzClient, FrendzClient>();
        services.AddHttpClient<FrendzClient>();
        services.AddScoped<ISigiloPayClient, SigiloPayClient>();
        services.AddHttpClient<SigiloPayClient>();
        services.AddScoped<IWintechClient, WintechClient>();
        services.AddHttpClient<WintechClient>();

        services.AddScoped<IFrendzApiKeysService, FrendzApiKeysService>();
        services.AddScoped<ISigiloPayApiKeysService, SigiloPayApiKeysService>();
        services.AddScoped<IWintechApiKeysService, WintechApiKeysService>();

        services.AddNexusGatewaysAdministrator();

        return services;
    }
}
