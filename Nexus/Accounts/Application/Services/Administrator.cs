using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Accounts.Application.Requests.Administrator;
using Nexus.Accounts.Application.Responses.Administrator;
using Nexus.Authorization.Application.Models;

namespace Nexus.Accounts.Application.Services;

public sealed class Administrator : IAdministrator
{
    private readonly IAdministratorAccessPolicy _policy;
    private readonly IAdministratorAccountSearchService _accountSearch;
    private readonly IAdministratorAccountCommandService _accountCommands;

    public Administrator(
        IAdministratorAccessPolicy policy,
        IAdministratorAccountSearchService accountSearch,
        IAdministratorAccountCommandService accountCommands)
    {
        _policy = policy;
        _accountSearch = accountSearch;
        _accountCommands = accountCommands;
    }

    public Task<IOperationResult<SearchAccountsResponse>> SearchAccountsAsync(
        RequesterIdentity identity,
        SearchAccountsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _accountSearch.SearchAccountsAsync(request), cancellationToken);
    }

    public Task<IOperationResult<GrantAccountRoleResponse>> GrantAccountRoleAsync(
        RequesterIdentity identity,
        GrantAccountRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _accountCommands.GrantAccountRoleAsync(request), cancellationToken);
    }

    public Task<IOperationResult<RevokeAccountRoleResponse>> RevokeAccountRoleAsync(
        RequesterIdentity identity,
        RevokeAccountRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _accountCommands.RevokeAccountRoleAsync(request), cancellationToken);
    }

    public Task<IOperationResult<GrantAccountPermissionResponse>> GrantAccountPermissionAsync(
        RequesterIdentity identity,
        GrantAccountPermissionRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _accountCommands.GrantAccountPermissionAsync(request), cancellationToken);
    }

    public Task<IOperationResult<RevokeAccountPermissionResponse>> RevokeAccountPermissionAsync(
        RequesterIdentity identity,
        RevokeAccountPermissionRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(identity, () => _accountCommands.RevokeAccountPermissionAsync(request), cancellationToken);
    }

    private async Task<IOperationResult<T>> ExecuteAsync<T>(
        RequesterIdentity identity,
        Func<Task<IResult<T>>> executeAsync,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

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
