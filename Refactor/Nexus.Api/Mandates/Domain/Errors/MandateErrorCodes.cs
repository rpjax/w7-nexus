namespace Refactor.Nexus.Api.Mandates.Domain.Errors;

public static class MandateErrorCodes
{
    public const string RequestBodyRequired = "Mandate.REQUEST_BODY_REQUIRED";
    public const string AccountNotFound = "Mandate.ACCOUNT_NOT_FOUND";
    public const string MemberNotFound = "Mandate.MEMBER_NOT_FOUND";
    public const string CapabilityUnknown = "Mandate.CAPABILITY_UNKNOWN";
    public const string CapabilityEmpty = "Mandate.CAPABILITY_EMPTY";
    public const string PresetUnknown = "Mandate.PRESET_UNKNOWN";
    public const string PresetAlreadyGranted = "Mandate.PRESET_ALREADY_GRANTED";
    public const string PresetNotGranted = "Mandate.PRESET_NOT_GRANTED";
    public const string GrantAlreadyExists = "Mandate.GRANT_ALREADY_EXISTS";
    public const string GrantNotFound = "Mandate.GRANT_NOT_FOUND";
    public const string AttenuationViolated = "Mandate.ATTENUATION_VIOLATED";
    public const string OperationScopeNotAvailable = "Mandate.OPERATION_SCOPE_NOT_AVAILABLE";
    public const string TipManagementConflict = "Mandate.TIP_MANAGEMENT_CONFLICT";
    public const string OperationNotFound = "Mandate.OPERATION_NOT_FOUND";
    public const string OperatorRequiresDeal = "Mandate.OPERATOR_REQUIRES_DEAL";
    public const string DealPercentsInvalid = "Mandate.DEAL_PERCENTS_INVALID";
    public const string DealSameParties = "Mandate.DEAL_SAME_PARTIES";
    public const string DealNotFound = "Mandate.DEAL_NOT_FOUND";
    public const string DealAlreadyClosed = "Mandate.DEAL_ALREADY_CLOSED";
    public const string DealRootRequiresAdmin = "Mandate.DEAL_ROOT_REQUIRES_ADMIN";
    public const string DealRecruiterLacksCapability = "Mandate.DEAL_RECRUITER_LACKS_CAPABILITY";
    public const string DealCannotCloseWhileOperatorPreset = "Mandate.DEAL_CANNOT_CLOSE_WHILE_OPERATOR_PRESET";
    public const string StakePercentageInvalid = "Mandate.STAKE_PERCENTAGE_INVALID";
    public const string StakeTotalExceedsHundred = "Mandate.STAKE_TOTAL_EXCEEDS_HUNDRED";
    public const string StakeNotFound = "Mandate.STAKE_NOT_FOUND";
    public const string Unauthorized = "Mandate.UNAUTHORIZED";
    public const string AttritionInvalid = "Mandate.ATTRITION_INVALID";
}
