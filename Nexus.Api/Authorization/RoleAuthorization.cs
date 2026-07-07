namespace Nexus.Authorization;

public static class RoleAuthorization
{
    public static bool IsGlobalAdministrator(IReadOnlyList<string> roles)
        => roles.Contains(Roles.Administrator, StringComparer.Ordinal);
}
