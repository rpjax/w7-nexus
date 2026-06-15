using Aidan.Core.Patterns;

namespace Nexus.Accounts.Application.Contracts;

public interface IAccountUpdater
{
    Task<IResult> UpdateUsernameAsync(string accountId, string newUsername);
    Task<IResult> UpdatePasswordAsync(string accountId, string newPassword);
    Task<IResult> AddRoleAsync(string accountId, string role);
    Task<IResult> RemoveRoleAsync(string accountId, string role);
    Task<IResult> ClearRolesAsync(string accountId);
    Task<IResult> AddPermissionAsync(string accountId, string permission);
    Task<IResult> RemovePermissionAsync(string accountId, string permission);
    Task<IResult> ClearPermissionsAsync(string accountId);
}
