using System.IdentityModel.Tokens.Jwt;
using Aidan.Core.Patterns;
using Microsoft.Extensions.Configuration;
using Refactor.Nexus.Api.Accounts.Application.Authorization.Administrator;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Queries.GetAccountById;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Authentication.Application.Models;
using Refactor.Nexus.Api.Authentication.Infrastructure.Tokens;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Authorization.Application.Models;
using Refactor.Nexus.Api.Authorization.Errors;
using Refactor.Nexus.Api.Tests.Fakes;

namespace Refactor.Nexus.Api.Tests;

public sealed class JwtPermissionAuthorizationTests
{
    [Fact]
    public void Access_token_carries_permission_claims()
    {
        var tokens = new JwtTokenService(Configuration()).GenerateTokens(new JwtTokenSubject
        {
            AccountId = Guid.NewGuid().ToString(),
            Username = "reader",
            Roles = [],
            Permissions = [Permissions.AccountsRead]
        });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        Assert.Contains(
            jwt.Claims,
            claim => claim.Type == Permissions.ClaimType && claim.Value == Permissions.AccountsRead);
    }

    [Fact]
    public async Task Accounts_read_permission_authorizes_get_account_without_administrator()
    {
        var accounts = new InMemoryAccountRepository();
        var target = await accounts.CreateAsync(Account.Create("target", "hash"));
        var readerId = Guid.NewGuid();

        var handler = new GetAccountByIdHandler(
            new StaticRequestContext(readerId.ToString(), [], [Permissions.AccountsRead]),
            new AdministratorAccessPolicy(),
            accounts);

        var result = await handler.HandleAsync(new GetAccountByIdQuery(target.Id.ToString()));

        Assert.False(result.IsFailure);
        Assert.NotNull(result.Value);
        Assert.Equal(target.Id.ToString(), result.Value.Account.Id);
    }

    [Fact]
    public async Task Missing_permission_and_role_is_unauthorized()
    {
        var accounts = new InMemoryAccountRepository();
        var target = await accounts.CreateAsync(Account.Create("target", "hash"));

        var handler = new GetAccountByIdHandler(
            new StaticRequestContext(Guid.NewGuid().ToString(), [], ["other.permission"]),
            new AdministratorAccessPolicy(),
            accounts);

        var result = await handler.HandleAsync(new GetAccountByIdQuery(target.Id.ToString()));

        Assert.True(result.IsFailure);
        Assert.False(result.IsAuthorized);
        Assert.Contains(result.AuthorizationErrors, error => error.Code == AuthorizationErrorCodes.NotAdministrator);
    }

    private static IConfiguration Configuration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "dev-signing-key-change-me-1234567890",
                ["Jwt:Issuer"] = "refactor-nexus",
                ["Jwt:Audience"] = "refactor-nexus"
            })
            .Build();

    private sealed class StaticRequestContext : IRequestContext
    {
        private readonly RequesterContext _context;

        public StaticRequestContext(string accountId, IReadOnlyList<string> roles, IReadOnlyList<string> permissions) =>
            _context = new RequesterContext(accountId, roles, permissions);

        public Task<IResult<RequesterContext>> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IResult<RequesterContext>>(Result<RequesterContext>.Success(_context));
    }
}
