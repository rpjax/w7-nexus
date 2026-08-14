namespace Refactor.Nexus.Api.Charging.Domain.Errors;

public static class ChargingErrorCodes
{
    public const string RequestBodyRequired = "Charging.REQUEST_BODY_REQUIRED";
    public const string Unauthorized = "Charging.UNAUTHORIZED";
    public const string ChargeNotFound = "Charging.CHARGE_NOT_FOUND";
    public const string RailNotFound = "Charging.RAIL_NOT_FOUND";
    public const string OperationNotFound = "Charging.OPERATION_NOT_FOUND";
    public const string OperationNotActive = "Charging.OPERATION_NOT_ACTIVE";
    public const string OperatorNotAssigned = "Charging.OPERATOR_NOT_ASSIGNED";
    public const string OrangeNotEligible = "Charging.ORANGE_NOT_ELIGIBLE";
    public const string NoQuota = "Charging.NO_QUOTA";
    public const string RailNotInSet = "Charging.RAIL_NOT_IN_SET";
    public const string RailBlocked = "Charging.RAIL_BLOCKED";
    public const string InvalidAmount = "Charging.INVALID_AMOUNT";
    public const string InvalidCut = "Charging.INVALID_CUT";
    public const string InvalidQuota = "Charging.INVALID_QUOTA";
    public const string InvalidTransition = "Charging.INVALID_TRANSITION";
    public const string AlreadyPaid = "Charging.ALREADY_PAID";
    public const string AlreadyMaterialized = "Charging.ALREADY_MATERIALIZED";
    public const string NotPaid = "Charging.NOT_PAID";
    public const string Terminal = "Charging.TERMINAL";
    public const string WebhookUnauthorized = "Charging.WEBHOOK_UNAUTHORIZED";
    public const string DealRequired = "Charging.DEAL_REQUIRED";
}
