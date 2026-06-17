using Microsoft.AspNetCore.DataProtection;
using Nexus.AppHost;
using Nexus.AppHost.Contracts;
using Nexus.Authentications.Application.Services.Models;

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
        services.AddControllers();
        services.AddHttpContextAccessor();
        services.AddSignalR();

        services.Configure<AppHostOptions>(builder.Configuration.GetSection(AppHostOptions.SectionName));
        services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AuthenticationOptions>(builder.Configuration.GetSection(AuthenticationOptions.SectionName));
        services.AddSingleton<IAppHostProvider, AppHostProvider>();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.SetIsOriginAllowed(_ => true)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()
                      .WithExposedHeaders("*");
            });
        });

        return services;
    }
}
