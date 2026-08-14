using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Domain.Errors;

namespace Refactor.Nexus.Api.Ledger.Application.UseCases.Shared;

internal static class LedgerGuards
{
    public static async Task<(RequesterContext? Requester, IOperationResult<T>? Failure)> AuthorizeAsync<T>(
        IRequestContext requestContext,
        ILedgerAccess access,
        CancellationToken cancellationToken)
    {
        var requesterResult = await requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return (null, OperationResult<T>.Failure(requesterResult.Errors));

        if (!Guid.TryParse(requester.AccountId, out var accountId))
            return (null, OperationResult<T>.Failure(Unauthorized("Identidade invalida.")));

        if (!await access.CanMaterializeAsync(accountId, cancellationToken)
            && !requester.Roles.Contains(Roles.Administrator, StringComparer.OrdinalIgnoreCase))
        {
            return (null, OperationResult<T>.Unauthorized(Unauthorized("Requer Admin ou Contador.")));
        }

        return (requester, null);
    }

    public static Error Unauthorized(string message) =>
        Error.Create().WithCode(LedgerErrorCodes.Unauthorized).WithMessage(message).Build();

    public static Error BodyRequired() =>
        Error.Create()
            .WithCode(LedgerErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisicao e obrigatorio.")
            .Build();
}
