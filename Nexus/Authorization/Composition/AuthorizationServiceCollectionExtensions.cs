using Nexus.Authorization.Application.Contracts;
using Nexus.Authorization.Application.Services;

namespace Nexus.Authorization.Composition;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddNexusAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IRequesterIdentityResolver, RequesterIdentityResolver>();

        return services;
    }
}
