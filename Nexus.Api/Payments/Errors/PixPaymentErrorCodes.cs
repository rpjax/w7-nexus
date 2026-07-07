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
    public const string OperatorNotFound = "PixPayment.OPERATOR_NOT_FOUND";
    public const string InvalidTransition = "PixPayment.INVALID_TRANSITION";
    public const string OperatorRequired = "PixPayment.OPERATOR_REQUIRED";
    public const string KillReasonRequired = "PixPayment.KILL_REASON_REQUIRED";
    public const string AlreadyKilled = "PixPayment.ALREADY_KILLED";
    public const string AmountInvalid = "PixPayment.AMOUNT_INVALID";
    public const string GatewayPaymentIdInvalid = "PixPayment.GATEWAY_PAYMENT_ID_INVALID";
    public const string StrawManNotFound = "PixPayment.STRAW_MAN_NOT_FOUND";
    public const string StrawManInvalid = "PixPayment.STRAW_MAN_INVALID";
    public const string StrawManRequired = "PixPayment.STRAW_MAN_REQUIRED";
    public const string StrawManRoleRequired = "PixPayment.STRAW_MAN_ROLE_REQUIRED";
    public const string StrawManAlreadyBound = "PixPayment.STRAW_MAN_ALREADY_BOUND";
    public const string PaymentIdAlreadyExists = "PixPayment.PAYMENT_ID_ALREADY_EXISTS";
    public const string GatewayPixFailed = "PixPayment.GATEWAY_PIX_FAILED";
    public const string NoGatewayServicesAvailable = "PixPayment.NO_GATEWAY_SERVICES_AVAILABLE";
    public const string TeamNotFound = "PixPayment.TEAM_NOT_FOUND";
    public const string TeamAmbiguous = "PixPayment.TEAM_AMBIGUOUS";
    public const string OperatorNotOnTeam = "PixPayment.OPERATOR_NOT_ON_TEAM";
    public const string ProfitShareRuleNotFound = "PixPayment.PROFIT_SHARE_RULE_NOT_FOUND";
    public const string ProfitShareRecipientsNotFound = "PixPayment.PROFIT_SHARE_RECIPIENTS_NOT_FOUND";
    public const string SplitsRequired = "PixPayment.SPLITS_REQUIRED";
    public const string InvalidSettlementTransition = "PixPayment.INVALID_SETTLEMENT_TRANSITION";
    public const string AlreadyWithdrawn = "PixPayment.ALREADY_WITHDRAWN";
    public const string AlreadyDistributed = "PixPayment.ALREADY_DISTRIBUTED";
    public const string DistributionRequiresWithdrawal = "PixPayment.DISTRIBUTION_REQUIRES_WITHDRAWAL";
    public const string InvalidDistributionTransition = "PixPayment.INVALID_DISTRIBUTION_TRANSITION";
    public const string SearchLimitInvalid = "Payment.SEARCH_LIMIT_INVALID";
    public const string SearchOffsetInvalid = "Payment.SEARCH_OFFSET_INVALID";
    public const string SearchKeywordTooLong = "Payment.SEARCH_KEYWORD_TOO_LONG";
    public const string RequestBodyRequired = "Payment.REQUEST_BODY_REQUIRED";
    public const string AccessDenied = "Payment.ACCESS_DENIED";
}
