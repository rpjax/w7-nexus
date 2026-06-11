using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Microsoft.AspNetCore.Http;
using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.ErrorCodes;
using Nexus.Actors.Contracts;

namespace Nexus.Authorization;

public sealed class AdministratorAccess : IAdministratorAccess
{
    private IHttpContextAccessor _httpContextAccessor { get; }
    private IAccountRepository _accounts { get; }
    private IAdministrator _administrator { get; }

    public AdministratorAccess(
        IHttpContextAccessor httpContextAccessor,
        IAccountRepository accounts,
        IAdministrator administrator)
    {
        _httpContextAccessor = httpContextAccessor;
        _accounts = accounts;
        _administrator = administrator;
    }

    public async Task<IAccessEvaluationResult<IAdministrator>> ResolveAsync(
        CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return AccessEvaluationResult<IAdministrator>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.IdentityRequired)
                .WithMessage("An authenticated identity is required.")
                .Build());
        }

        var accountId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(accountId))
        {
            return AccessEvaluationResult<IAdministrator>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.AccountIdClaimMissing)
                .WithMessage("Account identity claim is missing.")
                .Build());
        }

        var account = await _accounts.AsQueryable()
            .Where(a => a.Id == accountId)
            .FirstOrDefaultAsync();

        if (account is null)
        {
            return AccessEvaluationResult<IAdministrator>.Failure(Error.Create()
                .WithCode(AccountErrorCodes.AccountNotFound)
                .WithMessage($"Account '{accountId}' was not found.")
                .Build());
        }

        if (!account.Roles.Contains(Roles.Administrator, StringComparer.Ordinal))
        {
            return AccessEvaluationResult<IAdministrator>.Unauthorized(Error.Create()
                .WithCode(AuthorizationErrorCodes.NotAdministrator)
                .WithMessage("Administrator access is required.")
                .Build());
        }

        return AccessEvaluationResult<IAdministrator>.Authorized(_administrator);
    }
}
