using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Accounts.Domain.Errors;
using Refactor.Nexus.Api.Authorization;

namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Shared;

internal static class AccountAdministratorGuards
{
    public static async Task<Error?> EnsureNotLastAdministratorAsync(
        Account account,
        string? roleBeingRemoved,
        IAccountReadRepository accountReadRepository,
        CancellationToken cancellationToken)
    {
        var removingAdministrator = string.Equals(
            roleBeingRemoved,
            Roles.Administrator,
            StringComparison.OrdinalIgnoreCase);

        var disablingAdministrator = roleBeingRemoved is null && account.IsAdministrator;

        if (!removingAdministrator && !disablingAdministrator)
            return null;

        if (!account.IsAdministrator)
            return null;

        var administratorCount = await accountReadRepository.CountByRoleAsync(
            Roles.Administrator,
            cancellationToken);

        if (administratorCount <= 1)
        {
            return Error.Create()
                .WithCode(AccountErrorCodes.CannotRemoveLastAdministrator)
                .WithMessage("Nao e permitido remover o ultimo administrador do sistema.")
                .Build();
        }

        return null;
    }

    public static Error CannotDisableSelfError() =>
        Error.Create()
            .WithCode(AccountErrorCodes.CannotDisableSelf)
            .WithMessage("Voce nao pode desabilitar a propria conta.")
            .Build();

    public static Error CannotRevokeOwnAdministratorError() =>
        Error.Create()
            .WithCode(AccountErrorCodes.CannotRevokeOwnAdministrator)
            .WithMessage("Voce nao pode remover o preset Admin da propria conta. Peca a outro Admin.")
            .Build();

    public static Error NotFoundError(string accountId) =>
        Error.Create()
            .WithCode(AccountErrorCodes.AccountNotFound)
            .WithMessage($"A conta '{accountId}' nao foi encontrada.")
            .Build();

    public static Error RequestBodyRequiredError() =>
        Error.Create()
            .WithCode(AccountErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisicao e obrigatorio.")
            .Build();
}
