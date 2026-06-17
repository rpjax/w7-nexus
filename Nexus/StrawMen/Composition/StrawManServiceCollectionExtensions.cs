using Nexus.StrawMen.Application.Contracts;
using Nexus.StrawMen.Application.Services;

namespace Nexus.StrawMen.Composition;

public static class StrawManServiceCollectionExtensions
{
    public static IServiceCollection AddNexusStrawMan(this IServiceCollection services)
    {
        services.AddScoped<IStrawManAccessPolicy, StrawManAccessPolicy>();
        services.AddScoped<IStrawMan, StrawMan>();

        return services;
    }
}
