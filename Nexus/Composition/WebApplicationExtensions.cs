using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Nexus.Database.Models;
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

    public static async Task BackfillGatewayCredentialEnabledFieldsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var frendz = scope.ServiceProvider.GetRequiredService<IMongoCollection<FrendzApiCredentialsRecord>>();
        var sigilo = scope.ServiceProvider.GetRequiredService<IMongoCollection<SigiloPayApiCredentialsRecord>>();
        var wintech = scope.ServiceProvider.GetRequiredService<IMongoCollection<WintechApiCredentialsRecord>>();

        await frendz.UpdateManyAsync(
            Builders<FrendzApiCredentialsRecord>.Filter.Exists("enabled", false),
            Builders<FrendzApiCredentialsRecord>.Update.Set(x => x.Enabled, true));

        await sigilo.UpdateManyAsync(
            Builders<SigiloPayApiCredentialsRecord>.Filter.Exists("enabled", false),
            Builders<SigiloPayApiCredentialsRecord>.Update.Set(x => x.Enabled, true));

        await wintech.UpdateManyAsync(
            Builders<WintechApiCredentialsRecord>.Filter.Exists("enabled", false),
            Builders<WintechApiCredentialsRecord>.Update.Set(x => x.Enabled, true));
    }
}
