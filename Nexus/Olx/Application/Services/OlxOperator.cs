using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Olx.Application.Contracts;
using Nexus.Olx.Application.Requests;
using Nexus.Olx.Application.Requests.Operator;
using Nexus.Olx.Application.Responses;
using Nexus.Olx.Application.Responses.Operator;

namespace Nexus.Olx.Application.Services;

public sealed class OlxOperator : IOlxOperator
{
    private readonly IOlxOperatorAccessPolicy _policy;
    private readonly IAdSpoofCommandService _commands;
    private readonly IOlxOperatorAdSpoofSearchService _search;

    public OlxOperator(
        IOlxOperatorAccessPolicy policy,
        IAdSpoofCommandService commands,
        IOlxOperatorAdSpoofSearchService search)
    {
        _policy = policy;
        _commands = commands;
        _search = search;
    }

    public Task<IOperationResult<SearchAdSpoofsResponse>> SearchAdSpoofsAsync(
        RequesterIdentity identity,
        SearchAdSpoofsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            () => _search.SearchAdSpoofsAsync(identity, request),
            cancellationToken);
    }

    public Task<IOperationResult<ImpersonateAdResponse>> ImpersonateAdAsync(
        RequesterIdentity identity,
        ImpersonateAdRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            () => _commands.ImpersonateAdAsync(identity.AccountId, request, requireSelfOperator: true, cancellationToken),
            cancellationToken);
    }

    public Task<IOperationResult<UnimpersonateAdResponse>> UnimpersonateAdAsync(
        RequesterIdentity identity,
        UnimpersonateAdRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            () => _commands.UnimpersonateAdAsync(identity.AccountId, request, requireSelfOperator: true, cancellationToken),
            cancellationToken);
    }

    public Task<IOperationResult<UpdateAdDetailsSpoofResponse>> UpdateAdDetailsSpoofAsync(
        RequesterIdentity identity,
        UpdateAdDetailsSpoofRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            () => _commands.UpdateAdDetailsSpoofAsync(identity.AccountId, request, cancellationToken),
            cancellationToken);
    }

    private async Task<IOperationResult<T>> ExecuteAsync<T>(
        RequesterIdentity identity,
        Func<Task<IResult<T>>> executeAsync,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeOlxOperatorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<T>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<T>.Unauthorized(authorization.AuthorizationErrors);

        var result = await executeAsync();

        if (result.IsFailure)
            return OperationResult<T>.Failure(result.Errors);

        if (result.Value is not T value)
            return OperationResult<T>.Failure(result.Errors);

        return OperationResult<T>.Success(value);
    }
}
