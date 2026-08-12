namespace Refactor.Nexus.Api.Authentication.Domain.Errors;

public static class AuthenticationErrorCodes
{
    public const string RequestRequired = "Authentication.REQUEST_REQUIRED";
    public const string InvalidCredentials = "Authentication.INVALID_CREDENTIALS";
    public const string AccountNotFound = "Authentication.ACCOUNT_NOT_FOUND";
    public const string AccountDisabled = "Authentication.ACCOUNT_DISABLED";
    public const string InvalidToken = "Authentication.INVALID_TOKEN";
    public const string InvalidRefreshToken = "Authentication.INVALID_REFRESH_TOKEN";
}
