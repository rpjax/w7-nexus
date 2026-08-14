using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Refactor.Nexus.Api.Accounts.Composition;
using Refactor.Nexus.Api.Accounts.Infrastructure.Persistence;
using Refactor.Nexus.Api.Authentication.Composition;
using Refactor.Nexus.Api.Infrastructure.Persistence;
using Refactor.Nexus.Api.Journal.Composition;
using Refactor.Nexus.Api.Journal.Storage;
using Refactor.Nexus.Api.Mandates.Composition;
using Refactor.Nexus.Api.Mandates.Infrastructure.Persistence;
using Refactor.Nexus.Api.Operations.Composition;
using Refactor.Nexus.Api.Operations.Infrastructure.Persistence;
using Refactor.Nexus.Api.Charging.Composition;
using Refactor.Nexus.Api.Charging.Infrastructure.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Composition;
using Refactor.Nexus.Api.Ledger.Composition;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var corsOrigins = ResolveCorsOrigins(builder.Configuration);
var allowAnyOriginInDevelopment = builder.Environment.IsDevelopment() && corsOrigins.Length == 0;
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowAnyOriginInDevelopment)
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        policy.WithOrigins(corsOrigins.Length == 0 ? ["http://localhost"] : corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var signingKey = builder.Configuration["Jwt:SigningKey"] ?? "dev-signing-key-change-me-1234567890";
        var issuer = builder.Configuration["Jwt:Issuer"] ?? "refactor-nexus";
        var audience = builder.Configuration["Jwt:Audience"] ?? "refactor-nexus";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            RoleClaimType = "role",
            NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddRefactorAccounts(builder.Configuration);
builder.Services.AddRefactorOperations();
builder.Services.AddRefactorMandates();
builder.Services.AddRefactorCharging(builder.Configuration);
builder.Services.AddRefactorWorldAccounts();
builder.Services.AddRefactorLedger();
builder.Services.AddRefactorAuthentication();
builder.Services.AddJournal();
builder.Services.DiscoverJournalFacts();

var app = builder.Build();

await app.Services.InitializeAccountsDatabaseAsync();
await app.Services.InitializeMandatesDatabaseAsync();
await app.Services.InitializeOperationsDatabaseAsync();
await app.Services.InitializeChargingDatabaseAsync();
await app.Services.InitializeWorldAccountsAsync();
await app.Services.InitializeJournalDatabaseAsync();
await app.Services.SeedAdministratorIfNeededAsync();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static string[] ResolveCorsOrigins(IConfiguration configuration)
{
    var fromEnv = Environment.GetEnvironmentVariable("NEXUS_CORS_ORIGINS");
    if (!string.IsNullOrWhiteSpace(fromEnv))
    {
        return fromEnv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    return configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? [];
}
