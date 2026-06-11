using System.Text;
using Aidan.Mongo.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Nexus.AppHost;
using Nexus.AppHost.Contracts;
using Nexus.Actors;
using Nexus.Actors.Contracts;
using Nexus.Operations.Application;
using Nexus.Operations.Application.Contracts;
using Nexus.Database.Models;
using Nexus.Gateways.Wintech.Application;
using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.Frendz.Application;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.Application;
using Nexus.Gateways.Application.Contracts;
using Nexus.Payments.Application;
using Nexus.Payments.Application.Contracts;
using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.Application;
using Nexus.Authentication.Application;
using Nexus.Authentication.Application.Contracts;
using Nexus.Authorization.Services.Contracts;
using Nexus.Authorization.Services;
using Nexus.Authentication.Services.Models;

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
builder.Services.AddMongoCollection<TeamRecord>("teams");
builder.Services.AddMongoCollection<GatewayCredentialsGroupRecord>("gateway_credentials_groups");

builder.Services.Configure<AppHostOptions>(builder.Configuration.GetSection(AppHostOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<IAppHostProvider, AppHostProvider>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration is required.");
if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) || jwtOptions.SecretKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:SecretKey must be configured with at least 32 characters.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAdministratorAccess, AdministratorAccess>();
builder.Services.AddScoped<IOperationAdministratorAccess, OperationAdministratorAccess>();
builder.Services.AddScoped<ITeamLeaderAccess, TeamLeaderAccess>();

// Payment services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IGatewayPaymentWebhookService, GatewayPaymentWebhookService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IPaymentNotifier, SignalRPaymentNotifier>();
builder.Services.AddScoped<IAccountIdValidator, AccountIdValidator>();
builder.Services.AddScoped<IOperationRepository, OperationRepository>();
builder.Services.AddScoped<IOperationService, OperationService>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IAdministrator, Administrator>();
builder.Services.AddScoped<IOperationAdministrator, OperationAdministrator>();
builder.Services.AddScoped<ITeamLeader, TeamLeader>();
builder.Services.AddScoped<IFrendzApiCredentialsRepository, FrendzApiCredentialsRepository>();
builder.Services.AddScoped<ISigiloPayApiCredentialsRepository, SigiloPayApiCredentialsRepository>();
builder.Services.AddScoped<IWintechApiCredentialsRepository, WintechApiCredentialsRepository>();
builder.Services.AddScoped<IFrendzGatewayPixServiceFactory, FrendzGatewayPixServiceFactory>();
builder.Services.AddScoped<ISigiloPayGatewayPixServiceFactory, SigiloPayGatewayPixServiceFactory>();
builder.Services.AddScoped<IWintechGatewayPixServiceFactory, WintechGatewayPixServiceFactory>();
builder.Services.AddScoped<IGatewayCredentialsGroupRepository, GatewayCredentialsGroupRepository>();
builder.Services.AddScoped<IGatewayCredentialsGroupService, GatewayCredentialsGroupService>();
builder.Services.AddScoped<IGatewayCredentialsIdValidator, GatewayCredentialsIdValidator>();
builder.Services.AddScoped<IGatewayOrchestrator, GatewayOrchestrator>();
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

// Authentication services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUnauthenticatedUser, UnauthenticatedUser>();
builder.Services.AddScoped<ISignUpService, SignUpService>();
builder.Services.AddScoped<ISignInService, SignInService>();

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
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapGet("/", () => Results.Ok("Nexus API is running"));
app.MapHub<PaymentStatusHub>("/hubs/payment-status");
app.MapControllers();

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
