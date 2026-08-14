namespace Refactor.Nexus.Api.Ledger.Domain.Errors;

public static class LedgerErrorCodes
{
    public const string RequestBodyRequired = "Ledger.REQUEST_BODY_REQUIRED";
    public const string Unauthorized = "Ledger.UNAUTHORIZED";
    public const string ChargeNotFound = "Ledger.CHARGE_NOT_FOUND";
    public const string AccountNotFound = "Ledger.ACCOUNT_NOT_FOUND";
    public const string ClaimNotFound = "Ledger.CLAIM_NOT_FOUND";
    public const string InvalidAmount = "Ledger.INVALID_AMOUNT";
    public const string LandingLost = "Ledger.LANDING_LOST";
    public const string InvariantBroken = "Ledger.INVARIANT_BROKEN";
    public const string SplitFailed = "Ledger.SPLIT_FAILED";
    public const string ClaimNotActive = "Ledger.CLAIM_NOT_ACTIVE";
    public const string BundleEmpty = "Ledger.BUNDLE_EMPTY";
    public const string HopInvalid = "Ledger.HOP_INVALID";
    public const string CutAlreadyTaken = "Ledger.CUT_ALREADY_TAKEN";
    public const string OrangeNotEligible = "Ledger.ORANGE_NOT_ELIGIBLE";
    public const string NotPayout = "Ledger.NOT_PAYOUT";
    public const string AccountLost = "Ledger.ACCOUNT_LOST";
}
