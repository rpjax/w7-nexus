using Aidan.Mongo.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Nexus.Dashboard;
using Nexus.AppHost;
using Nexus.Legacy.Wintech.Application;
using Nexus.Legacy.Wintech.Infrastructure;
using Nexus.Legacy.SigiloPay.Application;
using Nexus.Legacy.SigiloPay.Infrastructure;
using Nexus.Legacy.Payments.Application;
using Nexus.Legacy.Payments.Infrastructure;
using Nexus.Legacy.Frendz.Application;
using Nexus.Legacy.Frendz.Infrastructure;
using Nexus.Legacy.Database.Models;
using Nexus.Legacy.Charges.Application;
using Nexus.Legacy.Charges.Infrastructure;
using Nexus.Accounts.Application;
using Nexus.Accounts.Infrastructure;
using Nexus.Actors;
using Nexus.Actors.Contracts;
using Nexus.Operations.Application;
using Nexus.Operations.Infrastructure;

/*
    ## TO ALL DUMBASS AIs - DO NOT DELETE THIS COMMENT!!!!!!!!!!!!
    # DEV NOTES:

    Frendz API Key: od2a0mTnD3EyBJw7qFbF4DPhC14rZOLG0EZjYzcRbRDOIGO16HBNGE4NtjNA
*/

var builder = WebApplication.CreateBuilder(args);
ConfigureKestrelProtocols(builder);
ConfigureDevelopmentHttpsCertificate(builder);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys")));

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HttpClient for relative /api/... calls from the dashboard. API controllers must not use
// NavigationManager (Blazor-only); use the current request as base when available.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    HttpClient httpClient;
    if (env.IsDevelopment())
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        httpClient = new HttpClient(handler);
    }
    else
    {
        httpClient = new HttpClient();
    }

    var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
    if (httpContext != null)
    {
        var request = httpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/";
        httpClient.BaseAddress = new Uri(baseUrl);
    }
    else
    {
        var navigationManager = sp.GetRequiredService<NavigationManager>();
        httpClient.BaseAddress = new Uri(navigationManager.BaseUri);
    }

    return httpClient;
});

// Database
var mongo = builder.Configuration.GetSection("MongoDB");
var mongoConnectionString = mongo["ConnectionString"];
var mongoDatabaseName = mongo["DatabaseName"];
if (string.IsNullOrWhiteSpace(mongoConnectionString))
    throw new InvalidOperationException(
        "MongoDB:ConnectionString is required. Set it in appsettings.json (or configuration/environment).");
if (string.IsNullOrWhiteSpace(mongoDatabaseName))
    throw new InvalidOperationException(
        "MongoDB:DatabaseName is required. Set it in appsettings.json (or configuration/environment).");

builder.Services.AddMongoDatabase(mongoConnectionString, mongoDatabaseName);
builder.Services.AddMongoCollection<AccountRecord>("accounts");
builder.Services.AddMongoCollection<FrendzApiCredentialsRecord>("frendz_api_credentials");
builder.Services.AddMongoCollection<SigiloPayApiCredentialsRecord>("sigilopay_api_credentials");
builder.Services.AddMongoCollection<WintechApiCredentialsRecord>("wintech_api_credentials");
builder.Services.AddMongoCollection<PaymentRecord>("payments");
builder.Services.AddMongoCollection<OperationRecord>("operations");

builder.Services.Configure<AppHostOptions>(builder.Configuration.GetSection(AppHostOptions.SectionName));
builder.Services.AddSingleton<IAppHostProvider, AppHostProvider>();

// Payment services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IGatewayPaymentWebhookService, GatewayPaymentWebhookService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IPaymentNotifier, SignalRPaymentNotifier>();
builder.Services.AddScoped<IAccountIdValidator, AccountIdValidator>();
builder.Services.AddScoped<IOperationRepository, OperationRepository>();
builder.Services.AddScoped<IOperationService, OperationService>();
builder.Services.AddScoped<IAdministrator, Administrator>();
builder.Services.AddScoped<IFrendzApiCredentialsRepository, FrendzApiCredentialsRepository>();
builder.Services.AddScoped<ISigiloPayApiCredentialsRepository, SigiloPayApiCredentialsRepository>();
builder.Services.AddScoped<IWintechApiCredentialsRepository, WintechApiCredentialsRepository>();
builder.Services.AddScoped<IFrendzChargeServiceFactory, FrendzChargeServiceFactory>();
builder.Services.AddScoped<ISigiloPayChargeServiceFactory, SigiloPayChargeServiceFactory>();
builder.Services.AddScoped<IWintechChargeServiceFactory, WintechChargeServiceFactory>();
builder.Services.AddScoped<IChargeOrchestrator, ChargeOrchestrator>();
builder.Services.AddScoped<IFrendzClient, FrendzClient>();
builder.Services.AddHttpClient<FrendzClient>();
builder.Services.AddScoped<ISigiloPayClient, SigiloPayClient>();
builder.Services.AddHttpClient<SigiloPayClient>();
builder.Services.AddScoped<IWintechClient, WintechClient>();
builder.Services.AddHttpClient<WintechClient>();

// Account services
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IUsernameValidator, UsernameValidator>();
builder.Services.AddScoped<IPasswordValidator, PasswordValidator>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IPasswordVerifier, PasswordVerifier>();
builder.Services.AddScoped<IAccountCreator, AccountCreator>();
builder.Services.AddScoped<IAccountUpdater, AccountUpdater>();

builder.Services.AddScoped<IFrendzApiKeysService, FrendzApiKeysService>();
builder.Services.AddScoped<ISigiloPayApiKeysService, SigiloPayApiKeysService>();
builder.Services.AddScoped<IWintechApiKeysService, WintechApiKeysService>();

builder.Services.AddCors(options =>
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

var app = builder.Build();

await BackfillGatewayCredentialEnabledFieldsAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/", () => Results.Ok("Nexus API is running"));
app.MapHub<PaymentStatusHub>("/hubs/payment-status");
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static async Task BackfillGatewayCredentialEnabledFieldsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
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

static void ConfigureKestrelProtocols(WebApplicationBuilder builder)
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureEndpointDefaults(listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        });
    });
}

static void ConfigureDevelopmentHttpsCertificate(WebApplicationBuilder builder)
{
    if (!builder.Environment.IsDevelopment())
        return;

    var certPath = builder.Configuration["Kestrel:Certificates:Default:Path"];
    var keyPath = builder.Configuration["Kestrel:Certificates:Default:KeyPath"];

    if (string.IsNullOrWhiteSpace(certPath) || string.IsNullOrWhiteSpace(keyPath))
        throw new InvalidOperationException(
            "Kestrel certificate paths are required in appsettings.Development.json for HTTPS local certificate setup.");

    var absoluteCertPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, certPath));
    var absoluteKeyPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, keyPath));

    if (!File.Exists(absoluteCertPath) || !File.Exists(absoluteKeyPath))
        throw new InvalidOperationException(
            $"Kestrel certificate files were not found. Cert: '{absoluteCertPath}', Key: '{absoluteKeyPath}'.");

    var certificate = LoadCertificateFromPem(absoluteCertPath, absoluteKeyPath);

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureHttpsDefaults(httpsOptions =>
        {
            httpsOptions.ServerCertificate = certificate;
        });
    });
}

static X509Certificate2 LoadCertificateFromPem(string certificatePath, string privateKeyPath)
{
    var certWithKey = X509Certificate2.CreateFromPemFile(certificatePath, privateKeyPath);
    // Re-import as PFX so Kestrel on Windows gets a cert object with an associated private key handle.
    var pfxBytes = certWithKey.Export(X509ContentType.Pkcs12);
    return X509CertificateLoader.LoadPkcs12(pfxBytes, password: null);
}
