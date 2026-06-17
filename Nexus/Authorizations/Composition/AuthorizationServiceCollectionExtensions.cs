using Nexus.Authorizations.Application.Contracts;
using Nexus.Authorizations.Application.Services;

namespace Nexus.Authorizations.Composition;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddNexusAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IRequesterIdentityResolver, RequesterIdentityResolver>();

        return services;
    }
}
