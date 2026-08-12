namespace Refactor.Nexus.Api.Authorization.Errors;

public static class AuthorizationErrorCodes
{
    public const string IdentityRequired = "Authorization.IDENTITY_REQUIRED";
    public const string AccountIdClaimMissing = "Authorization.ACCOUNT_ID_CLAIM_MISSING";
    public const string NotAdministrator = "Authorization.NOT_ADMINISTRATOR";
}
