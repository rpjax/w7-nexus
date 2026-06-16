namespace Nexus.Authorization;

public static class Roles
{
    public const string Administrator = "Administrator";
    public const string Operator = "Operator";
    public const string StrawMan = "StrawMan";
}

public static class Permissions
{
    public const string CreateOperatorAccount = "CreateOperatorAccount";
    public const string CreateAdministratorAccount = "CreateAdministratorAccount";
}
