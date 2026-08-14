using System.Text.Json.Serialization;
using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Accounts.Domain.Errors;
using Refactor.Nexus.Api.Accounts.Domain.Events;
using Refactor.Nexus.Api.Authorization;

namespace Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;

public sealed class Account
{
    private readonly List<string> _roles = [];
    private readonly List<string> _permissions = [];
    private readonly List<object> _uncommitted = [];

    public Account()
    {
    }

    public AccountId Id { get; private set; }
    public Guid PersistenceId => Id.Value;
    public string Username { get; private set; } = "";
    [JsonIgnore]
    public string PasswordHash { get; private set; } = "";
    public AccountStatus Status { get; private set; }
    public IReadOnlyList<string> Roles => _roles;
    public IReadOnlyList<string> Permissions => _permissions;
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    [JsonIgnore]
    public IReadOnlyList<object> UncommittedEvents => _uncommitted;

    public bool IsDisabled => Status == AccountStatus.Disabled;
    public bool IsAdministrator =>
        _roles.Contains(Authorization.Roles.Administrator, StringComparer.OrdinalIgnoreCase);

    public static Account Create(
        string username,
        string passwordHash,
        IReadOnlyCollection<string>? roles = null,
        IReadOnlyCollection<string>? permissions = null,
        Guid? actedBy = null)
    {
        var account = new Account();
        var now = DateTime.UtcNow;
        var id = AccountId.New();
        account.ApplyChange(new AccountRegistered(
            id.Value,
            username,
            (roles ?? []).ToArray(),
            (permissions ?? []).ToArray(),
            now,
            actedBy));
        account.PasswordHash = passwordHash;
        return account;
    }

    public void AttachPasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public void ClearUncommitted() => _uncommitted.Clear();

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
        var account = new Account();
        account.Apply(new AccountBackfilled(
            id.Value,
            username,
            status,
            roles.ToArray(),
            permissions.ToArray(),
            createdAt,
            lastUpdatedAt));
        account.AttachPasswordHash(passwordHash);
        return account;
    }

    public IResult ChangeUsername(string newUsername, Guid? actedBy = null)
    {
        if (string.IsNullOrWhiteSpace(newUsername))
            return Failure(AccountErrorCodes.UsernameEmpty, "O nome de usuario nao pode estar vazio.");

        if (string.Equals(Username, newUsername, StringComparison.OrdinalIgnoreCase))
            return Failure(AccountErrorCodes.UsernameUnchanged, "O novo nome de usuario e igual ao atual.");

        ApplyChange(new AccountUsernameChanged(Id.Value, Username, newUsername.Trim(), DateTime.UtcNow, actedBy));
        return Result.Success();
    }

    public IResult ChangePassword(string newPasswordHash, Guid? actedBy = null)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            return Failure(AccountErrorCodes.PasswordHashEmpty, "O hash da senha nao pode estar vazio.");

        PasswordHash = newPasswordHash;
        ApplyChange(new AccountPasswordChanged(Id.Value, DateTime.UtcNow, actedBy));
        return Result.Success();
    }

    public IResult Disable(Guid? actedBy = null)
    {
        if (Status == AccountStatus.Disabled)
            return Failure(AccountErrorCodes.AccountAlreadyDisabled, "A conta ja esta desabilitada.");

        ApplyChange(new AccountDisabled(Id.Value, DateTime.UtcNow, actedBy));
        return Result.Success();
    }

    public IResult Enable(Guid? actedBy = null)
    {
        if (Status == AccountStatus.Active)
            return Failure(AccountErrorCodes.AccountAlreadyActive, "A conta ja esta ativa.");

        ApplyChange(new AccountEnabled(Id.Value, DateTime.UtcNow, actedBy));
        return Result.Success();
    }

    public IResult AddRole(string role, Guid? actedBy = null)
    {
        if (string.IsNullOrWhiteSpace(role))
            return Failure(AccountErrorCodes.RoleEmpty, "A funcao nao pode estar vazia.");

        if (_roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            return Failure(AccountErrorCodes.RoleAlreadyExists, $"A funcao '{DescribeRole(role)}' ja esta atribuida a esta conta.");

        ApplyChange(new AccountAdministratorGranted(Id.Value, DateTime.UtcNow, actedBy));
        return Result.Success();
    }

    public IResult RemoveRole(string role, Guid? actedBy = null)
    {
        if (string.IsNullOrWhiteSpace(role))
            return Failure(AccountErrorCodes.RoleEmpty, "A funcao nao pode estar vazia.");

        if (!_roles.Exists(current => string.Equals(current, role, StringComparison.OrdinalIgnoreCase)))
            return Failure(AccountErrorCodes.RoleNotFound, $"A funcao '{DescribeRole(role)}' nao esta atribuida a esta conta.");

        ApplyChange(new AccountAdministratorRevoked(Id.Value, DateTime.UtcNow, actedBy));
        return Result.Success();
    }

    public IResult AddPermission(string permission, Guid? actedBy = null)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return Failure(AccountErrorCodes.PermissionEmpty, "A permissao nao pode estar vazia.");

        if (_permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            return Failure(AccountErrorCodes.PermissionAlreadyExists, $"A permissao '{permission}' ja esta atribuida a esta conta.");

        ApplyChange(new AccountPermissionGranted(Id.Value, permission.Trim(), DateTime.UtcNow, actedBy));
        return Result.Success();
    }

    public IResult RemovePermission(string permission, Guid? actedBy = null)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return Failure(AccountErrorCodes.PermissionEmpty, "A permissao nao pode estar vazia.");

        if (!_permissions.Exists(current => string.Equals(current, permission, StringComparison.OrdinalIgnoreCase)))
            return Failure(AccountErrorCodes.PermissionNotFound, $"A permissao '{permission}' nao esta atribuida a esta conta.");

        ApplyChange(new AccountPermissionRevoked(Id.Value, permission.Trim(), DateTime.UtcNow, actedBy));
        return Result.Success();
    }

    public void Apply(AccountRegistered e)
    {
        Id = new AccountId(e.AccountId);
        Username = e.Username;
        Status = AccountStatus.Active;
        ReplaceRoles(e.Roles);
        ReplacePermissions(e.Permissions);
        CreatedAt = e.OccurredAt;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(AccountBackfilled e)
    {
        Id = new AccountId(e.AccountId);
        Username = e.Username;
        Status = e.Status;
        ReplaceRoles(e.Roles);
        ReplacePermissions(e.Permissions);
        CreatedAt = e.CreatedAt;
        LastUpdatedAt = e.LastUpdatedAt;
    }

    public void Apply(AccountDisabled e)
    {
        Status = AccountStatus.Disabled;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(AccountEnabled e)
    {
        Status = AccountStatus.Active;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(AccountAdministratorGranted e)
    {
        if (!_roles.Contains(Authorization.Roles.Administrator, StringComparer.OrdinalIgnoreCase))
            _roles.Add(Authorization.Roles.Administrator);
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(AccountAdministratorRevoked e)
    {
        _roles.RemoveAll(r => string.Equals(r, Authorization.Roles.Administrator, StringComparison.OrdinalIgnoreCase));
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(AccountUsernameChanged e)
    {
        Username = e.ToUsername;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(AccountPasswordChanged e) => LastUpdatedAt = e.OccurredAt;

    public void Apply(AccountPermissionGranted e)
    {
        if (!_permissions.Contains(e.Permission, StringComparer.OrdinalIgnoreCase))
            _permissions.Add(e.Permission);
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(AccountPermissionRevoked e)
    {
        _permissions.RemoveAll(p => string.Equals(p, e.Permission, StringComparison.OrdinalIgnoreCase));
        LastUpdatedAt = e.OccurredAt;
    }

    private void ReplaceRoles(IEnumerable<string> roles)
    {
        _roles.Clear();
        _roles.AddRange(roles);
    }

    private void ReplacePermissions(IEnumerable<string> permissions)
    {
        _permissions.Clear();
        _permissions.AddRange(permissions);
    }

    private void ApplyChange(object @event)
    {
        switch (@event)
        {
            case AccountRegistered e: Apply(e); break;
            case AccountBackfilled e: Apply(e); break;
            case AccountDisabled e: Apply(e); break;
            case AccountEnabled e: Apply(e); break;
            case AccountAdministratorGranted e: Apply(e); break;
            case AccountAdministratorRevoked e: Apply(e); break;
            case AccountUsernameChanged e: Apply(e); break;
            case AccountPasswordChanged e: Apply(e); break;
            case AccountPermissionGranted e: Apply(e); break;
            case AccountPermissionRevoked e: Apply(e); break;
            default: throw new InvalidOperationException(@event.GetType().Name);
        }

        _uncommitted.Add(@event);
    }

    private static IResult Failure(string code, string message) =>
        Result.Failure(Error.Create().WithCode(code).WithMessage(message).Build());

    private static string DescribeRole(string role) =>
        string.Equals(role, Authorization.Roles.Administrator, StringComparison.OrdinalIgnoreCase)
            ? "administrador"
            : role;
}
