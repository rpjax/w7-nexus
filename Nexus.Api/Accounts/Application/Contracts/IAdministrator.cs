using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Requests.Administrator;
using Nexus.Accounts.Application.Responses.Administrator;
using Nexus.Authorization.Application.Models;

namespace Nexus.Accounts.Application.Contracts;

public interface IAdministrator
{
    Task<IOperationResult<CreateAccountResponse>> CreateAccountAsync(
        RequesterIdentity identity,
        CreateAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<SearchAccountsResponse>> SearchAccountsAsync(
        RequesterIdentity identity,
        SearchAccountsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<GrantAccountRoleResponse>> GrantAccountRoleAsync(
        RequesterIdentity identity,
        GrantAccountRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<RevokeAccountRoleResponse>> RevokeAccountRoleAsync(
        RequesterIdentity identity,
        RevokeAccountRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<GrantAccountPermissionResponse>> GrantAccountPermissionAsync(
        RequesterIdentity identity,
        GrantAccountPermissionRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<RevokeAccountPermissionResponse>> RevokeAccountPermissionAsync(
        RequesterIdentity identity,
        RevokeAccountPermissionRequest request,
        CancellationToken cancellationToken = default);
}
