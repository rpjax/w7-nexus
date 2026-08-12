using Refactor.Nexus.Api.Accounts.Application.DTOs;

namespace Refactor.Nexus.Api.Accounts.Presentation.Http.Administrator.Contracts;

public sealed class CreateAccountRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AccountType { get; set; } = "usuario";
}

public sealed class SearchAccountsRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public string? Role { get; set; }
}

public sealed class AccountRoleRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class AccountPermissionRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
}

public sealed class AccountIdRequest
{
    public string AccountId { get; set; } = string.Empty;
}

public sealed class ResetAccountPasswordRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class CreateAccountResponse
{
    public required AccountDetailsView Account { get; init; }
}

public sealed class SearchAccountsResponse
{
    public required int Offset { get; init; }
    public required int Limit { get; init; }
    public required int Total { get; init; }
    public IReadOnlyList<AccountDetailsView> Items { get; init; } = Array.Empty<AccountDetailsView>();
}

public sealed class GetAccountByIdResponse
{
    public required AccountDetailsView Account { get; init; }
}

public sealed class AccountMutationResponse
{
    public required AccountDetailsView Account { get; init; }
}

public sealed class GrantAccountRoleResponse;
public sealed class RevokeAccountRoleResponse;
public sealed class GrantAccountPermissionResponse;
public sealed class RevokeAccountPermissionResponse;
