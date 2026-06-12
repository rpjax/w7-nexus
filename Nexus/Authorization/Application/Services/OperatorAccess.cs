using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Microsoft.AspNetCore.Http;
using Nexus.Accounts.Application.Services.Contracts;
using Nexus.Accounts.Errors;
using Nexus.Actors.Contracts;
using Nexus.Authorization.Application.Models;
using Nexus.Authorization.Application.Services.Contracts;
using Nexus.Authorization.Errors;

namespace Nexus.Authorization.Application.Services;

public sealed class OperatorAccess : IOperatorAccess
{
    private IHttpContextAccessor _httpContextAccessor { get; }
    private IAccountRepository _accounts { get; }
    private IOperator _operator { get; }

    public OperatorAccess(
        IHttpContextAccessor httpContextAccessor,
        IAccountRepository accounts,
        IOperator @operator)
    {
        _httpContextAccessor = httpContextAccessor;
        _accounts = accounts;
        _operator = @operator;
    }

    public async Task<IAccessEvaluationResult<IOperator>> ResolveAsync(
        CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return AccessEvaluationResult<IOperator>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.IdentityRequired)
                .WithMessage("É necessário estar autenticado para realizar esta ação.")
                .Build());
        }

        var accountId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(accountId))
        {
            return AccessEvaluationResult<IOperator>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.AccountIdClaimMissing)
                .WithMessage("A identidade da conta não foi encontrada no token de acesso.")
                .Build());
        }

        var account = await _accounts.AsQueryable()
            .Where(a => a.Id == accountId)
            .FirstOrDefaultAsync();

        if (account is null)
        {
            return AccessEvaluationResult<IOperator>.Failure(Error.Create()
                .WithCode(AccountErrorCodes.AccountNotFound)
                .WithMessage($"A conta '{accountId}' não foi encontrada.")
                .Build());
        }

        if (!account.Roles.Contains(Roles.Operator, StringComparer.Ordinal)
            && !account.Roles.Contains(Roles.Administrator, StringComparer.Ordinal))
        {
            return AccessEvaluationResult<IOperator>.Unauthorized(Error.Create()
                .WithCode(AuthorizationErrorCodes.NotOperator)
                .WithMessage("Acesso de operador necessário para realizar esta ação.")
                .Build());
        }

        return AccessEvaluationResult<IOperator>.Authorized(_operator);
    }
}
