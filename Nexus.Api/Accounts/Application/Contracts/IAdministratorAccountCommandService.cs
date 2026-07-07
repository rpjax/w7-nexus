using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Requests.Administrator;
using Nexus.Accounts.Application.Responses.Administrator;

namespace Nexus.Accounts.Application.Contracts;

public interface IAdministratorAccountCommandService
{
    Task<IResult<CreateAccountResponse>> CreateAccountAsync(CreateAccountRequest request);
    Task<IResult<GrantAccountRoleResponse>> GrantAccountRoleAsync(GrantAccountRoleRequest request);
    Task<IResult<RevokeAccountRoleResponse>> RevokeAccountRoleAsync(RevokeAccountRoleRequest request);
    Task<IResult<GrantAccountPermissionResponse>> GrantAccountPermissionAsync(GrantAccountPermissionRequest request);
    Task<IResult<RevokeAccountPermissionResponse>> RevokeAccountPermissionAsync(RevokeAccountPermissionRequest request);
}
