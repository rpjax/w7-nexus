namespace Nexus.Gateways.Wintech.Errors;

public static class WintechErrorCodes
{
    public const string PublicKeyRequired = "Wintech.PUBLIC_KEY_REQUIRED";
    public const string SecretKeyRequired = "Wintech.SECRET_KEY_REQUIRED";
    public const string PublicKeyAlreadyExists = "Wintech.PUBLIC_KEY_ALREADY_EXISTS";
    public const string SecretKeyAlreadyExists = "Wintech.SECRET_KEY_ALREADY_EXISTS";
    public const string PublicKeyTooLong = "Wintech.PUBLIC_KEY_TOO_LONG";
    public const string SecretKeyTooLong = "Wintech.SECRET_KEY_TOO_LONG";
    public const string NameTooLong = "Wintech.NAME_TOO_LONG";
    public const string StrawManIdTooLong = "Wintech.STRAW_MAN_ID_TOO_LONG";
    public const string StrawManIdInvalid = "Wintech.STRAW_MAN_ID_INVALID";
    public const string StrawManAccountNotFound = "Wintech.STRAW_MAN_ACCOUNT_NOT_FOUND";
    public const string CredentialIdInvalid = "Wintech.CREDENTIAL_ID_INVALID";
    public const string CredentialNotFound = "Wintech.CREDENTIAL_NOT_FOUND";
}
