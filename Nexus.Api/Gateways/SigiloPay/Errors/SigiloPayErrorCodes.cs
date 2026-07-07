namespace Nexus.Gateways.SigiloPay.Errors;

public static class SigiloPayErrorCodes
{
    public const string PublicKeyRequired = "SigiloPay.PUBLIC_KEY_REQUIRED";
    public const string SecretKeyRequired = "SigiloPay.SECRET_KEY_REQUIRED";
    public const string PublicKeyAlreadyExists = "SigiloPay.PUBLIC_KEY_ALREADY_EXISTS";
    public const string SecretKeyAlreadyExists = "SigiloPay.SECRET_KEY_ALREADY_EXISTS";
    public const string PublicKeyTooLong = "SigiloPay.PUBLIC_KEY_TOO_LONG";
    public const string SecretKeyTooLong = "SigiloPay.SECRET_KEY_TOO_LONG";
    public const string NameTooLong = "SigiloPay.NAME_TOO_LONG";
    public const string StrawManIdTooLong = "SigiloPay.STRAW_MAN_ID_TOO_LONG";
    public const string StrawManIdInvalid = "SigiloPay.STRAW_MAN_ID_INVALID";
    public const string StrawManAccountNotFound = "SigiloPay.STRAW_MAN_ACCOUNT_NOT_FOUND";
    public const string CredentialIdInvalid = "SigiloPay.CREDENTIAL_ID_INVALID";
    public const string CredentialNotFound = "SigiloPay.CREDENTIAL_NOT_FOUND";
}
