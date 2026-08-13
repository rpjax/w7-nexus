using Refactor.Nexus.Api.Accounts.Application.Authorization.Administrator;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Queries;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Authenticated.Commands;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Security;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.CreateAccount;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.DisableAccount;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.EnableAccount;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.GrantAccountPermission;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.GrantAccountRole;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.ResetAccountPassword;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.RevokeAccountPermission;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.RevokeAccountRole;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Queries.GetAccountById;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Queries.SearchAccounts;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Authenticated.Commands.ChangeMyPassword;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Authenticated.Commands.ChangeMyUsername;
using Refactor.Nexus.Api.Accounts.Infrastructure.Persistence;
using Refactor.Nexus.Api.Accounts.Infrastructure.Persistence.Repositories;
using Refactor.Nexus.Api.Accounts.Infrastructure.Security;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Infrastructure.Persistence;

namespace Refactor.Nexus.Api.Accounts.Composition;

public static class AccountsServiceCollectionExtensions
{
    public static IServiceCollection AddRefactorAccounts(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<INpgsqlConnectionFactory, NpgsqlConnectionFactory>();
        services.AddScoped<PostgresAccountRepository>();
        services.AddScoped<IAccountRepository>(provider => provider.GetRequiredService<PostgresAccountRepository>());
        services.AddScoped<IAccountReadRepository>(provider => provider.GetRequiredService<PostgresAccountRepository>());
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IPasswordVerifier, BcryptPasswordVerifier>();
        services.AddScoped<IAdministratorCreationTokenService, ConfigurationAdministratorCreationTokenService>();
        services.AddScoped<IRequestContext, HttpRequestContext>();
        services.AddScoped<IAdministratorAccessPolicy, AdministratorAccessPolicy>();

        services.AddScoped<ICreateAccountUseCase, CreateAccountHandler>();
        services.AddScoped<ISearchAccountsUseCase, SearchAccountsHandler>();
        services.AddScoped<IGetAccountByIdUseCase, GetAccountByIdHandler>();
        services.AddScoped<IGrantAccountRoleUseCase, GrantAccountRoleHandler>();
        services.AddScoped<IRevokeAccountRoleUseCase, RevokeAccountRoleHandler>();
        services.AddScoped<IGrantAccountPermissionUseCase, GrantAccountPermissionHandler>();
        services.AddScoped<IRevokeAccountPermissionUseCase, RevokeAccountPermissionHandler>();
        services.AddScoped<IDisableAccountUseCase, DisableAccountHandler>();
        services.AddScoped<IEnableAccountUseCase, EnableAccountHandler>();
        services.AddScoped<IResetAccountPasswordUseCase, ResetAccountPasswordHandler>();

        services.AddScoped<IChangeMyPasswordUseCase, ChangeMyPasswordHandler>();
        services.AddScoped<IChangeMyUsernameUseCase, ChangeMyUsernameHandler>();

        services.AddScoped<IAccountsDatabaseInitializer, AccountsDatabaseInitializer>();
        services.AddScoped<SeedAdministrator>();

        return services;
    }
}
