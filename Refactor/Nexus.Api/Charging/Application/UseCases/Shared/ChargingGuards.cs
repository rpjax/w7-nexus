using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Charging.Domain.Errors;

namespace Refactor.Nexus.Api.Charging.Application.UseCases.Shared;

internal static class ChargingGuards
{
    public static async Task<(RequesterContext? Requester, IOperationResult<T>? Failure)> AuthorizeAdminAsync<T>(
        IRequestContext requestContext,
        IChargingMandateSnapshot mandates,
        CancellationToken cancellationToken)
    {
        var requesterResult = await requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return (null, OperationResult<T>.Failure(requesterResult.Errors));

        if (!Guid.TryParse(requester.AccountId, out var accountId))
            return (null, OperationResult<T>.Failure(Unauthorized("Identidade invalida.")));

        if (!await mandates.IsAdministratorAsync(accountId, cancellationToken)
            && !requester.Roles.Contains(Roles.Administrator, StringComparer.OrdinalIgnoreCase))
        {
            return (null, OperationResult<T>.Unauthorized(Unauthorized("Requer Admin.")));
        }

        return (requester, null);
    }

    public static Error Unauthorized(string message) =>
        Error.Create().WithCode(ChargingErrorCodes.Unauthorized).WithMessage(message).Build();

    public static Error BodyRequired() =>
        Error.Create()
            .WithCode(ChargingErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisicao e obrigatorio.")
            .Build();
}
