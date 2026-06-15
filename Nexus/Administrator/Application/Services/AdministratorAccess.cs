using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Microsoft.AspNetCore.Http;
using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.Errors;
using Nexus.Authorization;
using Nexus.Authorization.Errors;
using Nexus.Authorization.Application.Models;
using Nexus.Administrator.Application.Contracts;

namespace Nexus.Administrator.Application.Services;

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
                .WithMessage("É necessário estar autenticado para realizar esta ação.")
                .Build());
        }

        var accountId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(accountId))
        {
            return AccessEvaluationResult<IAdministrator>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.AccountIdClaimMissing)
                .WithMessage("A identidade da conta não foi encontrada no token de acesso.")
                .Build());
        }

        var account = await _accounts.AsQueryable()
            .Where(a => a.Id == accountId)
            .FirstOrDefaultAsync();

        if (account is null)
        {
            return AccessEvaluationResult<IAdministrator>.Failure(Error.Create()
                .WithCode(AccountErrorCodes.AccountNotFound)
                .WithMessage($"A conta '{accountId}' não foi encontrada.")
                .Build());
        }

        if (!account.Roles.Contains(Roles.Administrator, StringComparer.Ordinal))
        {
            return AccessEvaluationResult<IAdministrator>.Unauthorized(Error.Create()
                .WithCode(AuthorizationErrorCodes.NotAdministrator)
                .WithMessage("Acesso de administrador necessário para realizar esta ação.")
                .Build());
        }

        return AccessEvaluationResult<IAdministrator>.Authorized(_administrator);
    }
}
