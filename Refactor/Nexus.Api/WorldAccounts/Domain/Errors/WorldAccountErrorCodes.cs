namespace Refactor.Nexus.Api.WorldAccounts.Domain.Errors;

public static class WorldAccountErrorCodes
{
    public const string RequestBodyRequired = "WorldAccount.REQUEST_BODY_REQUIRED";
    public const string Unauthorized = "WorldAccount.UNAUTHORIZED";
    public const string NotFound = "WorldAccount.NOT_FOUND";
    public const string KindInvalid = "WorldAccount.KIND_INVALID";
    public const string LabelEmpty = "WorldAccount.LABEL_EMPTY";
    public const string OrangeRequired = "WorldAccount.ORANGE_REQUIRED";
    public const string OrangeNotEligible = "WorldAccount.ORANGE_NOT_ELIGIBLE";
    public const string OrangeNotAllowed = "WorldAccount.ORANGE_NOT_ALLOWED";
    public const string InvalidCut = "WorldAccount.INVALID_CUT";
    public const string InvalidQuota = "WorldAccount.INVALID_QUOTA";
    public const string InvalidAmount = "WorldAccount.INVALID_AMOUNT";
    public const string CurrencyEmpty = "WorldAccount.CURRENCY_EMPTY";
    public const string InsufficientBalance = "WorldAccount.INSUFFICIENT_BALANCE";
    public const string CannotEmit = "WorldAccount.CANNOT_EMIT";
    public const string NoQuota = "WorldAccount.NO_QUOTA";
    public const string EmissionBlocked = "WorldAccount.EMISSION_BLOCKED";
    public const string BalanceLost = "WorldAccount.BALANCE_LOST";
    public const string UseLostEndpoint = "WorldAccount.USE_LOST_ENDPOINT";
    public const string ObservationSeedOnly = "WorldAccount.OBSERVATION_SEED_ONLY";
    public const string NotGateway = "WorldAccount.NOT_GATEWAY";
}
