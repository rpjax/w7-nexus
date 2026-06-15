namespace Nexus.Accounts.Errors;

public static class AccountErrorCodes
{
    public const string RequestBodyRequired = "Account.REQUEST_BODY_REQUIRED";
    public const string SearchLimitInvalid = "Account.SEARCH_LIMIT_INVALID";
    public const string SearchOffsetInvalid = "Account.SEARCH_OFFSET_INVALID";
    public const string SearchKeywordTooLong = "Account.SEARCH_KEYWORD_TOO_LONG";

    public const string UsernameEmpty = "Account.USERNAME_EMPTY";
    public const string UsernameUnchanged = "Account.USERNAME_UNCHANGED";
    public const string UsernameAlreadyTaken = "Account.USERNAME_ALREADY_TAKEN";
    public const string UsernameInvalidFormat = "Account.USERNAME_INVALID_FORMAT";
    public const string PasswordHashEmpty = "Account.PASSWORD_HASH_EMPTY";
    public const string PasswordTooShort = "Account.PASSWORD_TOO_SHORT";
    public const string PasswordTooWeak = "Account.PASSWORD_TOO_WEAK";
    public const string RoleEmpty = "Account.ROLE_EMPTY";
    public const string RoleAlreadyExists = "Account.ROLE_ALREADY_EXISTS";
    public const string RoleNotFound = "Account.ROLE_NOT_FOUND";
    public const string PermissionEmpty = "Account.PERMISSION_EMPTY";
    public const string PermissionAlreadyExists = "Account.PERMISSION_ALREADY_EXISTS";
    public const string PermissionNotFound = "Account.PERMISSION_NOT_FOUND";
    public const string AccountNotFound = "Account.ACCOUNT_NOT_FOUND";
}
