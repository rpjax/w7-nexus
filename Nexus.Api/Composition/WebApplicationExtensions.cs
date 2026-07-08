using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Nexus.Payments.Presentation;

namespace Nexus.Composition;

public static class WebApplicationExtensions
{
    public static WebApplication UseNexusPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseForwardedHeaders();
        }

        var networkAccessName =
            app.Configuration["Monkeypatches:NetworkAccessName"] ?? CrossOriginHeaders.DefaultNetworkAccessName;
        var networkAccessId =
            app.Configuration["Monkeypatches:NetworkAccessId"] ?? CrossOriginHeaders.DefaultNetworkAccessId;

        app.UseOpenNetworkAccess();

        // TLS terminates at the reverse proxy in Staging/Production (Docker).
        // Local Development may still use Kestrel HTTPS via launchSettings / certs.
        if (app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                CrossOriginHeaders.Apply(ctx.Context, networkAccessName, networkAccessId);

                if (ctx.File.Name.Equals("service-worker.min.js", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Context.Response.Headers["Service-Worker-Allowed"] = "/";
                }
            },
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/", () => Results.Ok("Nexus API is running"));
        app.MapHub<PaymentStatusHub>("/hubs/payment-status");
        app.MapControllers();

        return app;
    }
}
