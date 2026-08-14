using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Domain.Errors;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Shared;

internal static class MandateAdministratorGuards
{
    public static async Task<IOperationResult<T>?> AuthorizeAdminAsync<T>(
        IRequestContext requestContext,
        IMandateAccessPolicy accessPolicy,
        CancellationToken cancellationToken)
    {
        var requesterResult = await requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<T>.Failure(requesterResult.Errors);

        var authorization = await accessPolicy.AuthorizeAdministratorAsync(requester.Roles, cancellationToken);
        if (authorization.IsFailure)
            return OperationResult<T>.Failure(authorization.Errors);
        if (!authorization.IsAuthorized)
            return OperationResult<T>.Unauthorized(authorization.AuthorizationErrors);

        return null;
    }

    public static async Task<(RequesterContext? Requester, IOperationResult<T>? Failure)> AuthorizeAdminWithRequesterAsync<T>(
        IRequestContext requestContext,
        IMandateAccessPolicy accessPolicy,
        CancellationToken cancellationToken)
    {
        var requesterResult = await requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return (null, OperationResult<T>.Failure(requesterResult.Errors));

        var authorization = await accessPolicy.AuthorizeAdministratorAsync(requester.Roles, cancellationToken);
        if (authorization.IsFailure)
            return (null, OperationResult<T>.Failure(authorization.Errors));
        if (!authorization.IsAuthorized)
            return (null, OperationResult<T>.Unauthorized(authorization.AuthorizationErrors));

        return (requester, null);
    }

    public static Error RequestBodyRequired() =>
        Error.Create()
            .WithCode(MandateErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisicao e obrigatorio.")
            .Build();

    public static Error AccountNotFound(string accountId) =>
        Error.Create()
            .WithCode(MandateErrorCodes.AccountNotFound)
            .WithMessage($"A conta '{accountId}' nao foi encontrada.")
            .Build();
}
