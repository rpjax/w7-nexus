namespace Nexus.AccountNodes.Errors;

public static class AccountNodeErrorCodes
{
    public const string SplitAccountIdInvalid = "AccountNode.SPLIT_ACCOUNT_ID_INVALID";
    public const string SplitPercentageInvalid = "AccountNode.SPLIT_PERCENTAGE_INVALID";
    public const string SplitAmountInvalid = "AccountNode.SPLIT_AMOUNT_INVALID";
    public const string SplitKindInvalid = "AccountNode.SPLIT_KIND_INVALID";
    public const string OriginOperationIdInvalid = "AccountNode.ORIGIN_OPERATION_ID_INVALID";
    public const string OriginStrawManIdInvalid = "AccountNode.ORIGIN_STRAW_MAN_ID_INVALID";
    public const string BalanceAmountInvalid = "AccountNode.BALANCE_AMOUNT_INVALID";
    public const string BalanceTransferIdInvalid = "AccountNode.BALANCE_TRANSFER_ID_INVALID";
    public const string BalanceSplitSnapshotRequired = "AccountNode.BALANCE_SPLIT_SNAPSHOT_REQUIRED";
    public const string BalanceAssetInvalid = "AccountNode.BALANCE_ASSET_INVALID";
    public const string BalanceChainInvalid = "AccountNode.BALANCE_CHAIN_INVALID";
    public const string BalanceIdInvalid = "AccountNode.BALANCE_ID_INVALID";
    public const string BalanceNotFound = "AccountNode.BALANCE_NOT_FOUND";
    public const string BalanceInsufficient = "AccountNode.BALANCE_INSUFFICIENT";
}

public static class BankAccountErrorCodes
{
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
}

public static class CryptoWalletErrorCodes
{
    public const string StrawManInvalid = "CryptoWallet.STRAW_MAN_INVALID";
    public const string StrawManNotFound = "CryptoWallet.STRAW_MAN_NOT_FOUND";
    public const string StrawManRoleRequired = "CryptoWallet.STRAW_MAN_ROLE_REQUIRED";
    public const string NamespaceInvalid = "CryptoWallet.NAMESPACE_INVALID";
    public const string AddressInvalid = "CryptoWallet.ADDRESS_INVALID";
    public const string AddressRequired = "CryptoWallet.ADDRESS_REQUIRED";
    public const string AddressTooLong = "CryptoWallet.ADDRESS_TOO_LONG";
    public const string DuplicateNamespace = "CryptoWallet.DUPLICATE_NAMESPACE";
    public const string NamespaceAddressMissing = "CryptoWallet.NAMESPACE_ADDRESS_MISSING";
    public const string MemoTooLong = "CryptoWallet.MEMO_TOO_LONG";
    public const string LabelTooLong = "CryptoWallet.LABEL_TOO_LONG";
    public const string CryptoWalletIdInvalid = "CryptoWallet.CRYPTO_WALLET_ID_INVALID";
    public const string CryptoWalletNotFound = "CryptoWallet.CRYPTO_WALLET_NOT_FOUND";
}
