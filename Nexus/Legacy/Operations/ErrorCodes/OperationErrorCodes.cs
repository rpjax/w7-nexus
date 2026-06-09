namespace Nexus.Legacy.Operations.ErrorCodes;

public static class OperationErrorCodes
{
    public const string RequestBodyRequired = "Operation.REQUEST_BODY_REQUIRED";

    public const string NameInvalid = "Operation.NAME_INVALID";
    public const string NameTooLong = "Operation.NAME_TOO_LONG";
    public const string NameAlreadyExists = "Operation.NAME_ALREADY_EXISTS";
    public const string DescriptionInvalid = "Operation.DESCRIPTION_INVALID";
    public const string OperationIdInvalid = "Operation.ID_INVALID";
    public const string OperationNotFound = "Operation.NOT_FOUND";
    public const string OperatorInvalid = "Operation.OPERATOR_INVALID";
    public const string OperatorAlreadyAssigned = "Operation.OPERATOR_ALREADY_ASSIGNED";
    public const string OperatorNotAssigned = "Operation.OPERATOR_NOT_ASSIGNED";
    public const string StrawManInvalid = "Operation.STRAW_MAN_INVALID";
    public const string StrawManAlreadyAssigned = "Operation.STRAW_MAN_ALREADY_ASSIGNED";
    public const string StrawManNotAssigned = "Operation.STRAW_MAN_NOT_ASSIGNED";

    public const string ManualChargeCredentialsDisabled = "Operation.MANUAL_CHARGE_CREDENTIALS_DISABLED";
    public const string ChargeCredentialInvalid = "Operation.CHARGE_CREDENTIAL_INVALID";
    public const string ChargeCredentialAlreadyAssigned = "Operation.CHARGE_CREDENTIAL_ALREADY_ASSIGNED";
    public const string ChargeCredentialNotAssigned = "Operation.CHARGE_CREDENTIAL_NOT_ASSIGNED";
}
