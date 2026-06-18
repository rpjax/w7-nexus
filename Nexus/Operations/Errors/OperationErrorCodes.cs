namespace Nexus.Operations.Errors;

public static class OperationErrorCodes
{
    public const string RequestBodyRequired = "Operation.REQUEST_BODY_REQUIRED";

    public const string NameInvalid = "Operation.NAME_INVALID";
    public const string NameTooLong = "Operation.NAME_TOO_LONG";
    public const string NameAlreadyExists = "Operation.NAME_ALREADY_EXISTS";
    public const string DescriptionInvalid = "Operation.DESCRIPTION_INVALID";
    public const string DescriptionTooLong = "Operation.DESCRIPTION_TOO_LONG";
    public const string SearchLimitInvalid = "Operation.SEARCH_LIMIT_INVALID";
    public const string SearchOffsetInvalid = "Operation.SEARCH_OFFSET_INVALID";
    public const string SearchKeywordTooLong = "Operation.SEARCH_KEYWORD_TOO_LONG";
    public const string OperationIdInvalid = "Operation.ID_INVALID";
    public const string OperationNotFound = "Operation.NOT_FOUND";
    public const string OperatorInvalid = "Operation.OPERATOR_INVALID";
    public const string OperatorAlreadyAssigned = "Operation.OPERATOR_ALREADY_ASSIGNED";
    public const string OperatorNotAssigned = "Operation.OPERATOR_NOT_ASSIGNED";
    public const string StrawManInvalid = "Operation.STRAW_MAN_INVALID";
    public const string StrawManAlreadyAssigned = "Operation.STRAW_MAN_ALREADY_ASSIGNED";
    public const string StrawManNotAssigned = "Operation.STRAW_MAN_NOT_ASSIGNED";

    public const string AdministratorInvalid = "Operation.ADMINISTRATOR_INVALID";
    public const string AdministratorAccountNotFound = "Operation.ADMINISTRATOR_ACCOUNT_NOT_FOUND";
    public const string AdministratorAlreadyAssigned = "Operation.ADMINISTRATOR_ALREADY_ASSIGNED";
    public const string AdministratorNotAssigned = "Operation.ADMINISTRATOR_NOT_ASSIGNED";

    public const string OperatorAccountNotFound = "Operation.OPERATOR_ACCOUNT_NOT_FOUND";
    public const string StrawManAccountNotFound = "Operation.STRAW_MAN_ACCOUNT_NOT_FOUND";

    public const string GatewayCredentialsGroupInvalid = "Operation.GATEWAY_CREDENTIALS_GROUP_INVALID";
    public const string GatewayCredentialsGroupAlreadyAssigned = "Operation.GATEWAY_CREDENTIALS_GROUP_ALREADY_ASSIGNED";
    public const string GatewayCredentialsGroupNotAssigned = "Operation.GATEWAY_CREDENTIALS_GROUP_NOT_ASSIGNED";
    public const string GatewayCredentialsGroupNotFound = "Operation.GATEWAY_CREDENTIALS_GROUP_NOT_FOUND";
    public const string GatewayCredentialsGroupStrategyMismatch = "Operation.GATEWAY_CREDENTIALS_GROUP_STRATEGY_MISMATCH";
    public const string GatewaySelectionStrategyInvalid = "Operation.GATEWAY_SELECTION_STRATEGY_INVALID";

    public const string ManualGatewayCredentialsDisabled = "Operation.MANUAL_GATEWAY_CREDENTIALS_DISABLED";
    public const string GatewayCredentialInvalid = "Operation.GATEWAY_CREDENTIAL_INVALID";
    public const string GatewayCredentialAlreadyAssigned = "Operation.GATEWAY_CREDENTIAL_ALREADY_ASSIGNED";
    public const string GatewayCredentialNotAssigned = "Operation.GATEWAY_CREDENTIAL_NOT_ASSIGNED";

    public const string ProfitShareStrategyNotSupported = "Operation.PROFIT_SHARE_STRATEGY_NOT_SUPPORTED";
}
