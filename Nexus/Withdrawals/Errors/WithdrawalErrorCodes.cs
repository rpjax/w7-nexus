namespace Nexus.Withdrawals.Errors;

public static class WithdrawalErrorCodes
{
    public const string OperationIdInvalid = "Withdrawal.OPERATION_ID_INVALID";
    public const string OperationNotFound = "Withdrawal.OPERATION_NOT_FOUND";
    public const string StrawManInvalid = "Withdrawal.STRAW_MAN_INVALID";
    public const string StrawManNotFound = "Withdrawal.STRAW_MAN_NOT_FOUND";
    public const string StrawManNotOnOperation = "Withdrawal.STRAW_MAN_NOT_ON_OPERATION";
    public const string StrawManRoleRequired = "Withdrawal.STRAW_MAN_ROLE_REQUIRED";
    public const string WithdrawalIdInvalid = "Withdrawal.WITHDRAWAL_ID_INVALID";
    public const string WithdrawalNotFound = "Withdrawal.WITHDRAWAL_NOT_FOUND";
    public const string TypeInvalid = "Withdrawal.TYPE_INVALID";
    public const string PaymentIdsRequired = "Withdrawal.PAYMENT_IDS_REQUIRED";
    public const string PaymentNotFound = "Withdrawal.PAYMENT_NOT_FOUND";
    public const string PaymentOperationMismatch = "Withdrawal.PAYMENT_OPERATION_MISMATCH";
    public const string PaymentNotPaid = "Withdrawal.PAYMENT_NOT_PAID";
    public const string PaymentAlreadyWithdrawn = "Withdrawal.PAYMENT_ALREADY_WITHDRAWN";
    public const string PaymentAlreadyLinked = "Withdrawal.PAYMENT_ALREADY_LINKED";
    public const string BankAccountRequired = "Withdrawal.BANK_ACCOUNT_REQUIRED";
    public const string CryptoWalletRequired = "Withdrawal.CRYPTO_WALLET_REQUIRED";
    public const string BankAccountMismatch = "Withdrawal.BANK_ACCOUNT_MISMATCH";
    public const string CryptoWalletMismatch = "Withdrawal.CRYPTO_WALLET_MISMATCH";
    public const string CostAmountInvalid = "Withdrawal.COST_AMOUNT_INVALID";
    public const string CostDescriptionTooLong = "Withdrawal.COST_DESCRIPTION_TOO_LONG";
    public const string PixProofTransactionIdTooLong = "Withdrawal.PIX_PROOF_TRANSACTION_ID_TOO_LONG";
    public const string PixProofAuthenticationCodeTooLong = "Withdrawal.PIX_PROOF_AUTHENTICATION_CODE_TOO_LONG";
    public const string CryptoProofTransactionIdTooLong = "Withdrawal.CRYPTO_PROOF_TRANSACTION_ID_TOO_LONG";
    public const string InvalidAggregateState = "Withdrawal.INVALID_AGGREGATE_STATE";
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
    public const string PixKeyTooLong = "BankAccount.PIX_KEY_TOO_LONG";
    public const string LabelTooLong = "BankAccount.LABEL_TOO_LONG";
    public const string BankAccountIdInvalid = "BankAccount.BANK_ACCOUNT_ID_INVALID";
    public const string BankAccountNotFound = "BankAccount.BANK_ACCOUNT_NOT_FOUND";
}

public static class CryptoWalletErrorCodes
{
    public const string StrawManInvalid = "CryptoWallet.STRAW_MAN_INVALID";
    public const string StrawManNotFound = "CryptoWallet.STRAW_MAN_NOT_FOUND";
    public const string StrawManRoleRequired = "CryptoWallet.STRAW_MAN_ROLE_REQUIRED";
    public const string ChainInvalid = "CryptoWallet.CHAIN_INVALID";
    public const string AssetInvalid = "CryptoWallet.ASSET_INVALID";
    public const string AddressInvalid = "CryptoWallet.ADDRESS_INVALID";
    public const string AddressTooLong = "CryptoWallet.ADDRESS_TOO_LONG";
    public const string MemoTooLong = "CryptoWallet.MEMO_TOO_LONG";
    public const string LabelTooLong = "CryptoWallet.LABEL_TOO_LONG";
    public const string CryptoWalletIdInvalid = "CryptoWallet.CRYPTO_WALLET_ID_INVALID";
    public const string CryptoWalletNotFound = "CryptoWallet.CRYPTO_WALLET_NOT_FOUND";
}
