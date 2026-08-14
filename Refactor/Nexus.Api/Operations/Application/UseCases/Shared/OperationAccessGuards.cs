using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Operations.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;
using Refactor.Nexus.Api.Operations.Domain.Errors;

namespace Refactor.Nexus.Api.Operations.Application.UseCases.Shared;

internal static class OperationAccessGuards
{
    public static async Task<(RequesterContext? Requester, IOperationResult<T>? Failure)> AuthorizeManageAsync<T>(
        IRequestContext requestContext,
        IMandateCapabilityGate capabilityGate,
        OperationId? operationId,
        CancellationToken cancellationToken)
    {
        var requesterResult = await requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return (null, OperationResult<T>.Failure(requesterResult.Errors));

        if (!Guid.TryParse(requester.AccountId, out var accountId))
        {
            return (null, OperationResult<T>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.Unauthorized)
                .WithMessage("Identidade invalida.")
                .Build()));
        }

        if (await capabilityGate.IsAdministratorAsync(accountId, cancellationToken))
            return (requester, null);

        // Create/list: requires gerir_operacao covering OperationAll (random Specific is covered by All).
        var probeId = operationId ?? OperationId.New();
        if (await capabilityGate.CanManageOperationAsync(accountId, probeId, cancellationToken))
            return (requester, null);

        return (null, OperationResult<T>.Unauthorized(Error.Create()
            .WithCode(OperationErrorCodes.Unauthorized)
            .WithMessage("Requer Admin ou gerir_operacao no escopo da Operacao.")
            .Build()));
    }

    public static Error RequestBodyRequired() =>
        Error.Create()
            .WithCode(OperationErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisicao e obrigatorio.")
            .Build();

    public static Error NotFound(string id) =>
        Error.Create()
            .WithCode(OperationErrorCodes.NotFound)
            .WithMessage($"Operacao '{id}' nao encontrada.")
            .Build();
}
