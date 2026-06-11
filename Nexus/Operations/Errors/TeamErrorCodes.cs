namespace Nexus.Operations.Errors;

public static class TeamErrorCodes
{
    public const string NameInvalid = "Team.NAME_INVALID";
    public const string NameTooLong = "Team.NAME_TOO_LONG";
    public const string NameAlreadyExists = "Team.NAME_ALREADY_EXISTS";
    public const string TeamIdInvalid = "Team.ID_INVALID";
    public const string TeamNotFound = "Team.NOT_FOUND";
    public const string OperationIdInvalid = "Team.OPERATION_ID_INVALID";
    public const string OperationNotFound = "Team.OPERATION_NOT_FOUND";

    public const string TeamLeaderInvalid = "Team.TEAM_LEADER_INVALID";
    public const string TeamLeaderAccountNotFound = "Team.TEAM_LEADER_ACCOUNT_NOT_FOUND";
    public const string TeamLeaderAlreadyAssigned = "Team.TEAM_LEADER_ALREADY_ASSIGNED";
    public const string TeamLeaderNotAssigned = "Team.TEAM_LEADER_NOT_ASSIGNED";

    public const string OperatorInvalid = "Team.OPERATOR_INVALID";
    public const string OperatorAccountNotFound = "Team.OPERATOR_ACCOUNT_NOT_FOUND";
    public const string OperatorAlreadyAssigned = "Team.OPERATOR_ALREADY_ASSIGNED";
    public const string OperatorAlreadyAssignedToAnotherTeam = "Team.OPERATOR_ALREADY_ASSIGNED_TO_ANOTHER_TEAM";
    public const string OperatorNotAssigned = "Team.OPERATOR_NOT_ASSIGNED";

    public const string StrawManInvalid = "Team.STRAW_MAN_INVALID";
    public const string StrawManAccountNotFound = "Team.STRAW_MAN_ACCOUNT_NOT_FOUND";
    public const string StrawManAlreadyAssigned = "Team.STRAW_MAN_ALREADY_ASSIGNED";
    public const string StrawManNotAssigned = "Team.STRAW_MAN_NOT_ASSIGNED";

    public const string GatewayCredentialsGroupInvalid = "Team.GATEWAY_CREDENTIALS_GROUP_INVALID";
    public const string GatewayCredentialsGroupAlreadyAssigned = "Team.GATEWAY_CREDENTIALS_GROUP_ALREADY_ASSIGNED";
    public const string GatewayCredentialsGroupNotAssigned = "Team.GATEWAY_CREDENTIALS_GROUP_NOT_ASSIGNED";
    public const string GatewayCredentialsGroupNotFound = "Team.GATEWAY_CREDENTIALS_GROUP_NOT_FOUND";
    public const string GatewayCredentialsGroupStrategyMismatch = "Team.GATEWAY_CREDENTIALS_GROUP_STRATEGY_MISMATCH";
    public const string GatewaySelectionStrategyInvalid = "Team.GATEWAY_SELECTION_STRATEGY_INVALID";

    public const string ManualGatewayCredentialsDisabled = "Team.MANUAL_GATEWAY_CREDENTIALS_DISABLED";
    public const string GatewayCredentialInvalid = "Team.GATEWAY_CREDENTIAL_INVALID";
    public const string GatewayCredentialAlreadyAssigned = "Team.GATEWAY_CREDENTIAL_ALREADY_ASSIGNED";
    public const string GatewayCredentialNotAssigned = "Team.GATEWAY_CREDENTIAL_NOT_ASSIGNED";

    public const string ProfitShareRuleEmpty = "Team.PROFIT_SHARE_RULE_EMPTY";
    public const string ProfitShareCutAccountInvalid = "Team.PROFIT_SHARE_CUT_ACCOUNT_INVALID";
    public const string ProfitShareCutAccountNotFound = "Team.PROFIT_SHARE_CUT_ACCOUNT_NOT_FOUND";
    public const string ProfitShareCutPercentageInvalid = "Team.PROFIT_SHARE_CUT_PERCENTAGE_INVALID";
    public const string ProfitShareCutDuplicateAccount = "Team.PROFIT_SHARE_CUT_DUPLICATE_ACCOUNT";
    public const string ProfitShareCutsMustTotal100Percent = "Team.PROFIT_SHARE_CUTS_MUST_TOTAL_100_PERCENT";
}
