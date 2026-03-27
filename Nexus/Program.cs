using Aidan.Mongo.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Cryptography.X509Certificates;
using Nexus.Accounts.Infrastructure;
using Nexus.Accounts.Application;
using Nexus.Database.Models;
using Nexus.Frendz.Application;
using Nexus.Frendz.Infrastructure;
using Nexus.Operations.Application;
using Nexus.Operations.Infrastructure;
using Nexus.Dashboard;
using Nexus.Payments.Infrastructure;
using Nexus.Payments.Application;
using Nexus.Charges.Application;
using Nexus.Charges.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
ConfigureDevelopmentKestrelWithLocalCertificate(builder);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
builder.Services.AddMongoCollection<PaymentRecord>("payments");
builder.Services.AddMongoCollection<OperationRecord>("operations");

// Payment services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IAccountIdValidator, AccountIdValidator>();
builder.Services.AddScoped<IOperationRepository, OperationRepository>();
builder.Services.AddScoped<IOperationService, OperationService>();
builder.Services.AddScoped<IFrendzApiCredentialsRepository, FrendzApiCredentialsRepository>();
builder.Services.AddScoped<IFrendzChargeServiceFactory, FrendzChargeServiceFactory>();
builder.Services.AddScoped<IChargeOrchestrator, ChargeOrchestrator>();
builder.Services.AddScoped<IFrendzClient, FrendzClient>();
builder.Services.AddHttpClient<FrendzClient>();

// Account services
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IUsernameValidator, UsernameValidator>();
builder.Services.AddScoped<IPasswordValidator, PasswordValidator>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IPasswordVerifier, PasswordVerifier>();
builder.Services.AddScoped<IAccountCreator, AccountCreator>();
builder.Services.AddScoped<IAccountUpdater, AccountUpdater>();

builder.Services.AddScoped<IFrendzApiKeysService, FrendzApiKeysService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/", () => Results.Ok("Nexus API is running"));
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await SeedFrendzApiCredentialsAsync(app.Services);
app.Run();

static void ConfigureDevelopmentKestrelWithLocalCertificate(WebApplicationBuilder builder)
{
    if (!builder.Environment.IsDevelopment())
        return;

    var certPath = builder.Configuration["Kestrel:Certificates:Default:Path"];
    var keyPath = builder.Configuration["Kestrel:Certificates:Default:KeyPath"];
    var httpsPort = int.TryParse(builder.Configuration["Kestrel:Endpoints:Https:Port"], out var parsedHttpsPort)
        ? parsedHttpsPort
        : 7254;
    var httpPort = int.TryParse(builder.Configuration["Kestrel:Endpoints:Http:Port"], out var parsedHttpPort)
        ? parsedHttpPort
        : 5113;

    if (string.IsNullOrWhiteSpace(certPath) || string.IsNullOrWhiteSpace(keyPath))
        throw new InvalidOperationException(
            "Kestrel certificate paths are required in appsettings.Development.json for HTTPS local certificate setup.");

    var absoluteCertPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, certPath));
    var absoluteKeyPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, keyPath));

    if (!File.Exists(absoluteCertPath) || !File.Exists(absoluteKeyPath))
        throw new InvalidOperationException(
            $"Kestrel certificate files were not found. Cert: '{absoluteCertPath}', Key: '{absoluteKeyPath}'.");

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureEndpointDefaults(listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        });

        options.ListenLocalhost(httpPort);
        options.ListenAnyIP(httpsPort, listenOptions =>
        {
            var certificate = LoadCertificateFromPem(absoluteCertPath, absoluteKeyPath);
            listenOptions.UseHttps(certificate);
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

static async Task SeedFrendzApiCredentialsAsync(IServiceProvider services)
{
    const string seedName = "default-dev-token";
    const string seedToken = "od2a0mTnD3EyBJw7qFbF4DPhC14rZOLG0EZjYzcRbRDOIGO16HBNGE4NtjNA";

    using var scope = services.CreateScope();
    var collection = scope.ServiceProvider.GetRequiredService<IMongoCollection<FrendzApiCredentialsRecord>>();

    var exists = await collection.Find(r => r.Token == seedToken).AnyAsync();
    if (exists)
        return;

    await collection.InsertOneAsync(new FrendzApiCredentialsRecord
    {
        Id = ObjectId.GenerateNewId(),
        Name = seedName,
        Token = seedToken
    });
}
