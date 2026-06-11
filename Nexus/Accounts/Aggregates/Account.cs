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
                .WithMessage("O nome de usuário não pode estar vazio.")
                .Build());

        if (string.Equals(Username, newUsername, StringComparison.OrdinalIgnoreCase))
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.UsernameUnchanged)
                .WithMessage("O novo nome de usuário é igual ao atual.")
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
                .WithMessage("O hash da senha não pode estar vazio.")
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
                .WithMessage("A função não pode estar vazia.")
                .Build());

        if (_roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.RoleAlreadyExists)
                .WithMessage($"A função '{DescribeRole(role)}' já está atribuída a esta conta.")
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
                .WithMessage("A função não pode estar vazia.")
                .Build());

        var index = _roles.FindIndex(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.RoleNotFound)
                .WithMessage($"A função '{DescribeRole(role)}' não está atribuída a esta conta.")
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
                .WithMessage("A permissão não pode estar vazia.")
                .Build());

        if (_permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.PermissionAlreadyExists)
                .WithMessage($"A permissão '{permission}' já está atribuída a esta conta.")
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
                .WithMessage("A permissão não pode estar vazia.")
                .Build());

        var index = _permissions.FindIndex(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return Result.Failure(Error.Create()
                .WithCode(AccountErrorCodes.PermissionNotFound)
                .WithMessage($"A permissão '{permission}' não está atribuída a esta conta.")
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

    private static string DescribeRole(string role) => role switch
    {
        global::Nexus.Authorization.Roles.Administrator => "administrador",
        global::Nexus.Authorization.Roles.Operator => "operador",
        _ => role,
    };
}
