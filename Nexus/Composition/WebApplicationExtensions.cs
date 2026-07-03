using Microsoft.AspNetCore.Http;
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

        var networkAccessName =
            app.Configuration["Monkeypatches:NetworkAccessName"] ?? CrossOriginHeaders.DefaultNetworkAccessName;
        var networkAccessId =
            app.Configuration["Monkeypatches:NetworkAccessId"] ?? CrossOriginHeaders.DefaultNetworkAccessId;

        app.UseOpenNetworkAccess();
        app.UseHttpsRedirection();
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
