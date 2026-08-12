using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Domain.Errors;
using Refactor.Nexus.Api.Authorization.Errors;

namespace Refactor.Nexus.Api.Authorization;

public sealed class HttpRequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAccountRepository _accountRepository;

    public HttpRequestContext(
        IHttpContextAccessor httpContextAccessor,
        IAccountRepository accountRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _accountRepository = accountRepository;
    }

    public async Task<IResult<RequesterContext>> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return Result<RequesterContext>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.IdentityRequired)
                .WithMessage("E necessario estar autenticado para realizar esta acao.")
                .Build());
        }

        var accountIdRaw = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Accounts.Domain.Aggregates.Account.AccountId.TryParse(accountIdRaw, out var accountId))
        {
            return Result<RequesterContext>.Failure(Error.Create()
                .WithCode(AuthorizationErrorCodes.AccountIdClaimMissing)
                .WithMessage("A identidade da conta nao foi encontrada no token de acesso.")
                .Build());
        }

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            return Result<RequesterContext>.Failure(Error.Create()
                .WithCode(AccountErrorCodes.AccountNotFound)
                .WithMessage($"A conta '{accountId}' nao foi encontrada.")
                .Build());
        }

        return Result<RequesterContext>.Success(new RequesterContext(
            account.Id.ToString(),
            account.Roles.ToArray(),
            account.Permissions.ToArray()));
    }
}
