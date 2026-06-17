using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.Application.Services;
using Nexus.Accounts.Infrastructure.Password;
using Nexus.Accounts.Infrastructure.Persistance;
using Nexus.Authentications.Application.Contracts;
using Nexus.Authentications.Application.Services;

namespace Nexus.Composition;

public static class AccountsServiceCollectionExtensions
{
    public static IServiceCollection AddNexusAccounts(this IServiceCollection services)
    {
        services.AddScoped<IAccountRepository, MongoAccountRepository>();
        services.AddScoped<IUsernameValidator, UsernameValidator>();
        services.AddScoped<IPasswordValidator, PasswordValidator>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IPasswordVerifier, PasswordVerifier>();
        services.AddScoped<IAccountCreator, AccountCreator>();
        services.AddScoped<IAccountUpdater, AccountUpdater>();

        services.AddSingleton<IAdministratorSignUpTokenService, AdministratorSignUpTokenService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUnauthenticatedUser, UnauthenticatedUser>();
        services.AddScoped<ISignUpService, SignUpService>();
        services.AddScoped<ISignInService, SignInService>();

        return services;
    }
}
