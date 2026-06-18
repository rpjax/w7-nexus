namespace Nexus.Payments.Errors;

public static class PixPaymentErrorCodes
{
    public const string GatewayInvalid = "PixPayment.GATEWAY_INVALID";
    public const string OperationIdInvalid = "PixPayment.OPERATION_ID_INVALID";
    public const string OperationNotFound = "PixPayment.OPERATION_NOT_FOUND";
    public const string PaymentIdInvalid = "PixPayment.PAYMENT_ID_INVALID";
    public const string PaymentNotFound = "PixPayment.PAYMENT_NOT_FOUND";
    public const string OperatorInvalid = "PixPayment.OPERATOR_INVALID";
    public const string OperatorAlreadyBound = "PixPayment.OPERATOR_ALREADY_BOUND";
    public const string OperatorAccountNotFound = "PixPayment.OPERATOR_ACCOUNT_NOT_FOUND";
    public const string InvalidTransition = "PixPayment.INVALID_TRANSITION";
    public const string OperatorRequired = "PixPayment.OPERATOR_REQUIRED";
    public const string DeathReasonRequired = "PixPayment.DEATH_REASON_REQUIRED";
    public const string AlreadyDead = "PixPayment.ALREADY_DEAD";
    public const string AmountInvalid = "PixPayment.AMOUNT_INVALID";
    public const string GatewayPaymentIdInvalid = "PixPayment.GATEWAY_PAYMENT_ID_INVALID";
    public const string StrawManAccountNotFound = "PixPayment.STRAW_MAN_ACCOUNT_NOT_FOUND";
    public const string StrawManInvalid = "PixPayment.STRAW_MAN_INVALID";
    public const string StrawManAlreadyBound = "PixPayment.STRAW_MAN_ALREADY_BOUND";
    public const string PaymentIdAlreadyExists = "PixPayment.PAYMENT_ID_ALREADY_EXISTS";
    public const string GatewayPixFailed = "PixPayment.GATEWAY_PIX_FAILED";
    public const string NoGatewayServicesAvailable = "PixPayment.NO_GATEWAY_SERVICES_AVAILABLE";
    public const string TeamNotFound = "PixPayment.TEAM_NOT_FOUND";
    public const string TeamIdRequired = "PixPayment.TEAM_ID_REQUIRED";
    public const string OperatorNotOnTeam = "PixPayment.OPERATOR_NOT_ON_TEAM";
    public const string ProfitShareRuleNotFound = "PixPayment.PROFIT_SHARE_RULE_NOT_FOUND";
    public const string SplitsRequired = "PixPayment.SPLITS_REQUIRED";
    public const string InvalidSettlementTransition = "PixPayment.INVALID_SETTLEMENT_TRANSITION";
    public const string AlreadyWithdrawn = "PixPayment.ALREADY_WITHDRAWN";
}
