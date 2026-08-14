using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Authorization.Administrator;
using Refactor.Nexus.Api.Accounts.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Domain.Errors;
using Refactor.Nexus.Api.Authorization;

namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Administrator.Commands.GrantAccountRole;

public sealed record GrantAccountRoleCommand(string AccountId, string Role);

public sealed class GrantAccountRoleResult;

public sealed class GrantAccountRoleHandler : IGrantAccountRoleUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IAdministratorAccessPolicy _accessPolicy;
    private readonly IAccountRepository _accountRepository;

    public GrantAccountRoleHandler(
        IRequestContext requestContext,
        IAdministratorAccessPolicy accessPolicy,
        IAccountRepository accountRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _accountRepository = accountRepository;
    }

    public async Task<IOperationResult<GrantAccountRoleResult>> HandleAsync(
        GrantAccountRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<GrantAccountRoleResult>.Failure(RequestBodyRequiredError());

        var access = await AuthorizeAsync(cancellationToken);
        if (access is not null)
            return access;

        if (!AccountId.TryParse(command.AccountId, out var accountId))
            return OperationResult<GrantAccountRoleResult>.Failure(NotFoundError(command.AccountId));

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            return OperationResult<GrantAccountRoleResult>.Failure(NotFoundError(command.AccountId));

        if (!Roles.IsGrantable(command.Role))
        {
            return OperationResult<GrantAccountRoleResult>.Failure(Error.Create()
                .WithCode(AccountErrorCodes.RoleNotGrantable)
                .WithMessage($"A funcao '{command.Role}' nao e concedivel nesta etapa. Apenas '{Roles.Administrator}' pode ser atribuida.")
                .Build());
        }

        var mutation = account.AddRole(command.Role);
        if (mutation.IsFailure)
            return OperationResult<GrantAccountRoleResult>.Failure(mutation.Errors);

        await _accountRepository.UpdateAsync(account, cancellationToken);
        return OperationResult<GrantAccountRoleResult>.Success(new GrantAccountRoleResult());
    }

    private async Task<IOperationResult<GrantAccountRoleResult>?> AuthorizeAsync(CancellationToken cancellationToken)
    {
        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<GrantAccountRoleResult>.Failure(requesterResult.Errors);

        var authorization = await _accessPolicy.AuthorizeAsync(requester.Roles, cancellationToken);
        if (authorization.IsFailure)
            return OperationResult<GrantAccountRoleResult>.Failure(authorization.Errors);
        if (!authorization.IsAuthorized)
            return OperationResult<GrantAccountRoleResult>.Unauthorized(authorization.AuthorizationErrors);

        return null;
    }

    private static Error RequestBodyRequiredError() =>
        Error.Create()
            .WithCode(AccountErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisicao e obrigatorio.")
            .Build();

    private static Error NotFoundError(string accountId) =>
        Error.Create()
            .WithCode(AccountErrorCodes.AccountNotFound)
            .WithMessage($"A conta '{accountId}' nao foi encontrada.")
            .Build();
}
