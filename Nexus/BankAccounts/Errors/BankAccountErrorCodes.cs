namespace Nexus.BankAccounts.Errors;

public static class BankAccountErrorCodes
{
    public const string OwnerInvalid = "BankAccount.OWNER_INVALID";
    public const string OwnerNotFound = "BankAccount.OWNER_NOT_FOUND";
    public const string StrawManInvalid = "BankAccount.STRAW_MAN_INVALID";
    public const string StrawManNotFound = "BankAccount.STRAW_MAN_NOT_FOUND";
    public const string StrawManRoleRequired = "BankAccount.STRAW_MAN_ROLE_REQUIRED";
    public const string BankInvalid = "BankAccount.BANK_INVALID";
    public const string AgencyInvalid = "BankAccount.AGENCY_INVALID";
    public const string AgencyTooLong = "BankAccount.AGENCY_TOO_LONG";
    public const string AccountNumberInvalid = "BankAccount.ACCOUNT_NUMBER_INVALID";
    public const string AccountNumberTooLong = "BankAccount.ACCOUNT_NUMBER_TOO_LONG";
    public const string AccountDigitTooLong = "BankAccount.ACCOUNT_DIGIT_TOO_LONG";
    public const string AccountTypeInvalid = "BankAccount.ACCOUNT_TYPE_INVALID";
    public const string LabelTooLong = "BankAccount.LABEL_TOO_LONG";
    public const string BankAccountIdInvalid = "BankAccount.BANK_ACCOUNT_ID_INVALID";
    public const string BankAccountNotFound = "BankAccount.BANK_ACCOUNT_NOT_FOUND";
    public const string BalanceAmountInvalid = "BankAccount.BALANCE_AMOUNT_INVALID";
    public const string BalanceTransferIdInvalid = "BankAccount.BALANCE_TRANSFER_ID_INVALID";
    public const string BalanceSplitsRequired = "BankAccount.BALANCE_SPLITS_REQUIRED";
    public const string BalanceIdInvalid = "BankAccount.BALANCE_ID_INVALID";
    public const string BalanceNotFound = "BankAccount.BALANCE_NOT_FOUND";
    public const string BalanceInsufficient = "BankAccount.BALANCE_INSUFFICIENT";
    public const string SplitAccountIdInvalid = "BankAccount.SPLIT_ACCOUNT_ID_INVALID";
    public const string SplitPercentageInvalid = "BankAccount.SPLIT_PERCENTAGE_INVALID";
    public const string SplitAmountInvalid = "BankAccount.SPLIT_AMOUNT_INVALID";
    public const string SplitKindInvalid = "BankAccount.SPLIT_KIND_INVALID";
    public const string OriginOperationIdInvalid = "BankAccount.ORIGIN_OPERATION_ID_INVALID";
    public const string OriginStrawManIdInvalid = "BankAccount.ORIGIN_STRAW_MAN_ID_INVALID";
}
