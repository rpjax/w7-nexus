using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Accounts.Domain.Errors;
using Refactor.Nexus.Api.Authorization;

namespace Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;

/// <summary>
/// Identidade de login da organização (uma pessoa). <see cref="Username"/> é o <c>handle</c>
/// de produto: único no deploy e nunca reutilizado após troca (reserva em retired_handles).
/// </summary>
public sealed class Account
{
    private readonly List<string> _roles;
    private readonly List<string> _permissions;

    private Account(
        AccountId id,
        string username,
        string passwordHash,
        AccountStatus status,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        DateTime createdAt,
        DateTime lastUpdatedAt)
    {
        Id = id;
        Username = username;
        PasswordHash = passwordHash;
        Status = status;
        _roles = roles.ToList();
        _permissions = permissions.ToList();
        CreatedAt = createdAt;
        LastUpdatedAt = lastUpdatedAt;
    }

    public AccountId Id { get; }

    /// <summary>Handle de login (canônico de produto). Infra persiste como username.</summary>
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public AccountStatus Status { get; private set; }
    public IReadOnlyList<string> Roles => _roles;
    public IReadOnlyList<string> Permissions => _permissions;
    public DateTime CreatedAt { get; }
    public DateTime LastUpdatedAt { get; private set; }

    public bool IsDisabled => Status == AccountStatus.Disabled;
    public bool IsAdministrator =>
        _roles.Contains(Authorization.Roles.Administrator, StringComparer.OrdinalIgnoreCase);

    public static Account Create(
        string username,
        string passwordHash,
        IReadOnlyCollection<string>? roles = null,
        IReadOnlyCollection<string>? permissions = null)
    {
        var now = DateTime.UtcNow;

        return new Account(
            AccountId.New(),
            username,
            passwordHash,
            AccountStatus.Active,
            roles ?? Array.Empty<string>(),
            permissions ?? Array.Empty<string>(),
            now,
            now);
    }

    public static Account Rehydrate(
        AccountId id,
        string username,
        string passwordHash,
        AccountStatus status,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        DateTime createdAt,
        DateTime lastUpdatedAt)
    {
        return new Account(
            id,
            username,
            passwordHash,
            status,
            roles,
            permissions,
            createdAt,
            lastUpdatedAt);
    }

    public IResult ChangeUsername(string newUsername)
    {
        if (string.IsNullOrWhiteSpace(newUsername))
            return Failure(AccountErrorCodes.UsernameEmpty, "O nome de usuario nao pode estar vazio.");

        if (string.Equals(Username, newUsername, StringComparison.OrdinalIgnoreCase))
            return Failure(AccountErrorCodes.UsernameUnchanged, "O novo nome de usuario e igual ao atual.");

        Username = newUsername.Trim();
        Touch();
        return Result.Success();
    }

    public IResult ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            return Failure(AccountErrorCodes.PasswordHashEmpty, "O hash da senha nao pode estar vazio.");

        PasswordHash = newPasswordHash;
        Touch();
        return Result.Success();
    }

    public IResult Disable()
    {
        if (Status == AccountStatus.Disabled)
            return Failure(AccountErrorCodes.AccountAlreadyDisabled, "A conta ja esta desabilitada.");

        Status = AccountStatus.Disabled;
        Touch();
        return Result.Success();
    }

    public IResult Enable()
    {
        if (Status == AccountStatus.Active)
            return Failure(AccountErrorCodes.AccountAlreadyActive, "A conta ja esta ativa.");

        Status = AccountStatus.Active;
        Touch();
        return Result.Success();
    }

    public IResult AddRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return Failure(AccountErrorCodes.RoleEmpty, "A funcao nao pode estar vazia.");

        if (_roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            return Failure(AccountErrorCodes.RoleAlreadyExists, $"A funcao '{DescribeRole(role)}' ja esta atribuida a esta conta.");

        _roles.Add(role.Trim());
        Touch();
        return Result.Success();
    }

    public IResult RemoveRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return Failure(AccountErrorCodes.RoleEmpty, "A funcao nao pode estar vazia.");

        var index = _roles.FindIndex(current => string.Equals(current, role, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return Failure(AccountErrorCodes.RoleNotFound, $"A funcao '{DescribeRole(role)}' nao esta atribuida a esta conta.");

        _roles.RemoveAt(index);
        Touch();
        return Result.Success();
    }

    public IResult AddPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return Failure(AccountErrorCodes.PermissionEmpty, "A permissao nao pode estar vazia.");

        if (_permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            return Failure(AccountErrorCodes.PermissionAlreadyExists, $"A permissao '{permission}' ja esta atribuida a esta conta.");

        _permissions.Add(permission.Trim());
        Touch();
        return Result.Success();
    }

    public IResult RemovePermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return Failure(AccountErrorCodes.PermissionEmpty, "A permissao nao pode estar vazia.");

        var index = _permissions.FindIndex(current => string.Equals(current, permission, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return Failure(AccountErrorCodes.PermissionNotFound, $"A permissao '{permission}' nao esta atribuida a esta conta.");

        _permissions.RemoveAt(index);
        Touch();
        return Result.Success();
    }

    private void Touch() => LastUpdatedAt = DateTime.UtcNow;

    private static IResult Failure(string code, string message) =>
        Result.Failure(Error.Create()
            .WithCode(code)
            .WithMessage(message)
            .Build());

    private static string DescribeRole(string role) =>
        string.Equals(role, Authorization.Roles.Administrator, StringComparison.OrdinalIgnoreCase)
            ? "administrador"
            : role;
}
