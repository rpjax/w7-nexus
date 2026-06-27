namespace Nexus.Authorization.Errors;

public static class AuthorizationErrorCodes
{
    public const string IdentityRequired = "Authorization.IDENTITY_REQUIRED";
    public const string AccountIdClaimMissing = "Authorization.ACCOUNT_ID_CLAIM_MISSING";
    public const string NotAdministrator = "Authorization.NOT_ADMINISTRATOR";
    public const string NotOperationAdministrator = "Authorization.NOT_OPERATION_ADMINISTRATOR";
    public const string NotTeamLeader = "Authorization.NOT_TEAM_LEADER";
    public const string NotOperator = "Authorization.NOT_OPERATOR";
    public const string NotStrawMan = "Authorization.NOT_STRAW_MAN";
    public const string NotOlxOperator = "Authorization.NOT_OLX_OPERATOR";
    public const string ScopeMismatch = "Authorization.SCOPE_MISMATCH";
}
