using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Microsoft.AspNetCore.Http;
using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.Errors;
using Nexus.Authorizations.Application.Contracts;
using Nexus.Authorizations.Application.Models;
using Nexus.Authorizations.Errors;

namespace Nexus.Authorizations.Application.Services;

public sealed class RequesterIdentityResolver : IRequesterIdentityResolver
{
    private IHttpContextAccessor _httpContextAccessor { get; }
    private IAccountRepository _accounts { get; }

    public RequesterIdentityResolver(
        IHttpContextAccessor httpContextAccessor,
        IAccountRepository accounts)
    {
        _httpContextAccessor = httpContextAccessor;
        _accounts = accounts;
    }

    public async Task<IResult<RequesterIdentity>> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return Result<RequesterIdentity>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.IdentityRequired)
                .WithMessage("É necessário estar autenticado para realizar esta ação.")
                .Build());
        }

        var accountId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Result<RequesterIdentity>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.AccountIdClaimMissing)
                .WithMessage("A identidade da conta não foi encontrada no token de acesso.")
                .Build());
        }

        var account = await _accounts.AsQueryable()
            .Where(a => a.Id == accountId.Trim())
            .FirstOrDefaultAsync();

        if (account is null)
        {
            return Result<RequesterIdentity>.Failure(Error.Create()
                .WithCode(AccountErrorCodes.AccountNotFound)
                .WithMessage($"A conta '{accountId.Trim()}' não foi encontrada.")
                .Build());
        }

        return Result<RequesterIdentity>.Success(RequesterIdentity.FromAccount(account));
    }
}
