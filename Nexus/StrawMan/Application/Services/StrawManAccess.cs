using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Microsoft.AspNetCore.Http;
using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.Errors;
using Nexus.Authorization;
using Nexus.Authorization.Application.Models;
using Nexus.Authorization.Errors;
using Nexus.StrawMan.Application.Contracts;

namespace Nexus.StrawMan.Application.Services;

public sealed class StrawManAccess : IStrawManAccess
{
    private IHttpContextAccessor _httpContextAccessor { get; }
    private IAccountRepository _accounts { get; }
    private IStrawMan _strawMan { get; }

    public StrawManAccess(
        IHttpContextAccessor httpContextAccessor,
        IAccountRepository accounts,
        IStrawMan strawMan)
    {
        _httpContextAccessor = httpContextAccessor;
        _accounts = accounts;
        _strawMan = strawMan;
    }

    public async Task<IAccessEvaluationResult<IStrawMan>> ResolveAsync(
        CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return AccessEvaluationResult<IStrawMan>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.IdentityRequired)
                .WithMessage("É necessário estar autenticado para realizar esta ação.")
                .Build());
        }

        var accountId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(accountId))
        {
            return AccessEvaluationResult<IStrawMan>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.AccountIdClaimMissing)
                .WithMessage("A identidade da conta não foi encontrada no token de acesso.")
                .Build());
        }

        var account = await _accounts.AsQueryable()
            .Where(a => a.Id == accountId)
            .FirstOrDefaultAsync();

        if (account is null)
        {
            return AccessEvaluationResult<IStrawMan>.Failure(Error.Create()
                .WithCode(AccountErrorCodes.AccountNotFound)
                .WithMessage($"A conta '{accountId}' não foi encontrada.")
                .Build());
        }

        if (!RoleAuthorization.IsGlobalAdministrator(account.Roles)
            && !account.Roles.Contains(Roles.StrawMan, StringComparer.Ordinal))
        {
            return AccessEvaluationResult<IStrawMan>.Unauthorized(Error.Create()
                .WithCode(AuthorizationErrorCodes.NotStrawMan)
                .WithMessage("Acesso de laranja necessário para realizar esta ação.")
                .Build());
        }

        return AccessEvaluationResult<IStrawMan>.Authorized(_strawMan);
    }
}
