namespace Refactor.Nexus.Api.Authorization;

/// <summary>
/// JWT claim type <see cref="ClaimType"/> is emitted at sign-in.
/// Capabilities live primarily in Mandates; this list is the Accounts
/// permission channel that the token actually authorizes.
/// </summary>
public static class Permissions
{
    public const string ClaimType = "permission";
    public const string AccountsRead = "accounts.read";

    public static bool Has(IReadOnlyList<string> granted, string permission) =>
        granted.Contains(permission, StringComparer.OrdinalIgnoreCase);
}
