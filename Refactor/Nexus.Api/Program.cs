using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Refactor.Nexus.Api.Accounts.Composition;
using Refactor.Nexus.Api.Authentication.Composition;
using Refactor.Nexus.Api.Infrastructure.Persistence;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddRefactorAccounts(builder.Configuration);
builder.Services.AddRefactorAuthentication();

var app = builder.Build();

await app.Services.InitializeAccountsDatabaseAsync();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
