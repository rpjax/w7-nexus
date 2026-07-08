using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Nexus.AppHost;
using Nexus.AppHost.Contracts;
using Nexus.Authentication.Application.Services.Models;

namespace Nexus.Composition;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddNexusInfrastructure(
        this IServiceCollection services,
        WebApplicationBuilder builder)
    {
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(
                Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys")));

        services.AddOpenApi();
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        services.AddHttpContextAccessor();
        services.AddSignalR();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.Configure<AppHostOptions>(builder.Configuration.GetSection(AppHostOptions.SectionName));
        services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AuthenticationOptions>(builder.Configuration.GetSection(AuthenticationOptions.SectionName));
        services.AddSingleton<IAppHostProvider, AppHostProvider>();

        return services;
    }
}
