using Microsoft.Extensions.DependencyInjection;
using Nexus.Scripts.Application.Contracts;
using Nexus.Scripts.Application.Services;
using Nexus.Scripts.Infrastructure.Persistance;

namespace Nexus.Scripts.Composition;

public static class ScriptsServiceCollectionExtensions
{
    public static IServiceCollection AddNexusScripts(this IServiceCollection services)
    {
        services.AddSingleton<ScriptCache>();

        services.AddScoped<IScriptRepository, MongoScriptRepository>();
        services.AddScoped<IReleaseRepository, MongoReleaseRepository>();
        services.AddScoped<IScriptResolver, ScriptResolver>();
        services.AddScoped<IScriptAdministrator, ScriptAdministrator>();
        services.AddScoped<IAdministratorAccessPolicy, AdministratorAccessPolicy>();

        return services;
    }
}
