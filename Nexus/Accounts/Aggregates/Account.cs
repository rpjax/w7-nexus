using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Errors;

namespace Nexus.Accounts.Aggregates;

public sealed class Account
{
    private readonly List<string> _roles;
    private readonly List<string> _permissions;

    public string Id { get; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public IReadOnlyList<string> Roles => _roles;
    public IReadOnlyList<string> Permissions => _permissions;
    public DateTime CreatedAt { get; }
    public DateTime LastUpdatedAt { get; private set; }

    /// <summary>Constructor for creation. The creator service enforces invariants before calling.</summary>
    internal Account(
        string Id,
        string Username,
        string PasswordHash,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions)
    {
        var now = DateTime.UtcNow;
        this.Id = Id;
        this.Username = Username;
        this.PasswordHash = PasswordHash;
        _roles = Roles.ToList();
        _permissions = Permissions.ToList();
        this.CreatedAt = now;
        this.LastUpdatedAt = now;
    }

    /// <summary>Constructor for rehydration from persistence.</summary>
    internal Account(
        string Id,
        string Username,
        string PasswordHash,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions,
        DateTime CreatedAt,
        DateTime LastUpdatedAt)
    {
        this.Id = Id;
        this.Username = Username;
        this.PasswordHash = PasswordHash;
        _roles = Roles.ToList();
        _permissions = Permissions.ToList();
        this.CreatedAt = CreatedAt;
        this.LastUpdatedAt = LastUpdatedAt;
    }

    public IResult ChangeUsername(string newUsername)
    {
        if (string.IsNullOrWhiteSpace(newUsername))
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.UsernameEmpty)
                .WithMessage("Username cannot be empty")
                .Build());

        if (string.Equals(Username, newUsername, StringComparison.OrdinalIgnoreCase))
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.UsernameUnchanged)
                .WithMessage("New username is the same as current")
                .Build());

        Username = newUsername;
        LastUpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public IResult ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.PasswordHashEmpty)
                .WithMessage("Password hash cannot be empty")
                .Build());

        PasswordHash = newPasswordHash;
        LastUpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public IResult AddRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.RoleEmpty)
                .WithMessage("Role cannot be empty")
                .Build());

        if (_roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.RoleAlreadyExists)
                .WithMessage($"Role '{role}' already exists")
                .Build());

        _roles.Add(role);
        LastUpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public IResult RemoveRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.RoleEmpty)
                .WithMessage("Role cannot be empty")
                .Build());

        var index = _roles.FindIndex(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.RoleNotFound)
                .WithMessage($"Role '{role}' was not found")
                .Build());

        _roles.RemoveAt(index);
        LastUpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public IResult ClearRoles()
    {
        _roles.Clear();
        LastUpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public IResult AddPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.PermissionEmpty)
                .WithMessage("Permission cannot be empty")
                .Build());

        if (_permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.PermissionAlreadyExists)
                .WithMessage($"Permission '{permission}' already exists")
                .Build());

        _permissions.Add(permission);
        LastUpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public IResult RemovePermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.PermissionEmpty)
                .WithMessage("Permission cannot be empty")
                .Build());

        var index = _permissions.FindIndex(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.PermissionNotFound)
                .WithMessage($"Permission '{permission}' was not found")
                .Build());

        _permissions.RemoveAt(index);
        LastUpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public IResult ClearPermissions()
    {
        _permissions.Clear();
        LastUpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
