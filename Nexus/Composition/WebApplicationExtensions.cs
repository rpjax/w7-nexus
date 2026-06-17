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

        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHttpsRedirection();
        app.MapGet("/", () => Results.Ok("Nexus API is running"));
        app.MapHub<PaymentStatusHub>("/hubs/payment-status");
        app.MapControllers();

        return app;
    }
}
