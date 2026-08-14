using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.Errors;
using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;

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

    public static async Task<(RequesterContext? Requester, IOperationResult<T>? Failure)> AuthorizeGrantorAsync<T>(
        IRequestContext requestContext,
        IAccountDirectory accounts,
        IMemberMandateReadRepository mandates,
        CancellationToken cancellationToken)
    {
        var requesterResult = await requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return (null, OperationResult<T>.Failure(requesterResult.Errors));

        if (!MemberId.TryParse(requester.AccountId, out var grantorId))
            return (null, OperationResult<T>.Unauthorized(Unauthorized("Identidade invalida.")));

        if (await accounts.IsAdministratorAsync(grantorId, cancellationToken)
            || requester.Roles.Contains(Roles.Administrator, StringComparer.OrdinalIgnoreCase))
        {
            return (requester, null);
        }

        var mandate = await mandates.GetByMemberIdAsync(grantorId, cancellationToken);
        if (mandate is not null && CanGrantNested(mandate))
            return (requester, null);

        return (null, OperationResult<T>.Unauthorized(Unauthorized("Requer Admin ou conceder_mandato.")));
    }

    public static bool CanGrantNested(Domain.Aggregates.MemberMandate.MemberMandate mandate) =>
        mandate.HasCapability(Capabilities.ConcederMandato, MandateScope.Organization())
        || mandate.HasCapability(Capabilities.ConcederMandato, MandateScope.CarteiraDirect())
        || mandate.HasCapability(Capabilities.ConcederMandato, MandateScope.OperationAll())
        || mandate.Grants.Any(g =>
            string.Equals(g.Capability, Capabilities.ConcederMandato, StringComparison.Ordinal));

    public static Error Unauthorized(string message) =>
        Error.Create().WithCode(MandateErrorCodes.Unauthorized).WithMessage(message).Build();

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
