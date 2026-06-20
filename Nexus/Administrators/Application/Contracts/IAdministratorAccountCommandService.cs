using Aidan.Core.Patterns;
using Nexus.Administrators.Application.Requests;
using Nexus.Administrators.Application.Responses;

namespace Nexus.Administrators.Application.Contracts;

public interface IAdministratorAccountCommandService
{
    Task<IResult<GrantAccountRoleResponse>> GrantAccountRoleAsync(GrantAccountRoleRequest request);
    Task<IResult<RevokeAccountRoleResponse>> RevokeAccountRoleAsync(RevokeAccountRoleRequest request);
    Task<IResult<GrantAccountPermissionResponse>> GrantAccountPermissionAsync(GrantAccountPermissionRequest request);
    Task<IResult<RevokeAccountPermissionResponse>> RevokeAccountPermissionAsync(RevokeAccountPermissionRequest request);
}
