using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.WorldAccounts.Domain.Errors;

namespace Refactor.Nexus.Api.WorldAccounts.Application.UseCases.Shared;

internal static class WorldAccountGuards
{
    public static async Task<(RequesterContext? Requester, IOperationResult<T>? Failure)> AuthorizeManageAsync<T>(
        IRequestContext requestContext,
        IWorldAccountAccess access,
        CancellationToken cancellationToken)
    {
        var requesterResult = await requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return (null, OperationResult<T>.Failure(requesterResult.Errors));

        if (!Guid.TryParse(requester.AccountId, out var accountId))
            return (null, OperationResult<T>.Failure(Unauthorized("Identidade invalida.")));

        if (!await access.CanManageGatewaysAsync(accountId, cancellationToken)
            && !requester.Roles.Contains(Roles.Administrator, StringComparer.OrdinalIgnoreCase))
        {
            return (null, OperationResult<T>.Unauthorized(Unauthorized("Requer Admin ou gerir_gateways.")));
        }

        return (requester, null);
    }

    public static Error Unauthorized(string message) =>
        Error.Create().WithCode(WorldAccountErrorCodes.Unauthorized).WithMessage(message).Build();

    public static Error BodyRequired() =>
        Error.Create()
            .WithCode(WorldAccountErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisicao e obrigatorio.")
            .Build();

    public static Error NotFound(string id) =>
        Error.Create()
            .WithCode(WorldAccountErrorCodes.NotFound)
            .WithMessage($"Conta '{id}' nao encontrada.")
            .Build();
}
