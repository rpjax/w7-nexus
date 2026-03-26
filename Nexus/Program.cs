using Aidan.Mongo.Extensions;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Accounts.Infrastructure;
using Nexus.Accounts.Application;
using Nexus.Database.Models;
using Nexus.Frendz.Application;
using Nexus.Frendz.Infrastructure;
using Nexus.Operations.Application;
using Nexus.Operations.Infrastructure;
using Nexus.Payments.Infrastructure;
using Nexus.Payments.Application;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddMongoCollection<PixPaymentRecord>("pix_payments");
builder.Services.AddMongoCollection<OperationRecord>("operations");

// Payment services
builder.Services.AddScoped<IPixPaymentService, PixPaymentService>();
builder.Services.AddScoped<IPixPaymentRepository, PixPaymentRepository>();
builder.Services.AddScoped<IAccountIdValidator, AccountIdValidator>();
builder.Services.AddScoped<IOperationRepository, OperationRepository>();
builder.Services.AddScoped<IOperationService, OperationService>();

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

app.MapGet("/", () => Results.Ok("Nexus API is running"));
app.MapControllers();

await SeedFrendzApiCredentialsAsync(app.Services);
app.Run();

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
