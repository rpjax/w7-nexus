namespace Nexus.Transfers.Errors;

public static class TransferErrorCodes
{
    public const string StrawManInvalid = "Transfer.STRAW_MAN_INVALID";
    public const string StrawManNotFound = "Transfer.STRAW_MAN_NOT_FOUND";
    public const string StrawManRoleRequired = "Transfer.STRAW_MAN_ROLE_REQUIRED";
    public const string TransferIdInvalid = "Transfer.TRANSFER_ID_INVALID";
    public const string TransferNotFound = "Transfer.TRANSFER_NOT_FOUND";
    public const string TypeInvalid = "Transfer.TYPE_INVALID";
    public const string PaymentIdsRequired = "Transfer.PAYMENT_IDS_REQUIRED";
    public const string PaymentNotFound = "Transfer.PAYMENT_NOT_FOUND";
    public const string PaymentStrawManNotBound = "Transfer.PAYMENT_STRAW_MAN_NOT_BOUND";
    public const string PaymentStrawManMismatch = "Transfer.PAYMENT_STRAW_MAN_MISMATCH";
    public const string PaymentNotPaid = "Transfer.PAYMENT_NOT_PAID";
    public const string PaymentAlreadyWithdrawn = "Transfer.PAYMENT_ALREADY_WITHDRAWN";
    public const string PaymentAlreadyLinked = "Transfer.PAYMENT_ALREADY_LINKED";
    public const string PaymentSplitMismatch = "Transfer.PAYMENT_SPLIT_MISMATCH";
    public const string BankAccountRequired = "Transfer.BANK_ACCOUNT_REQUIRED";
    public const string CryptoWalletRequired = "Transfer.CRYPTO_WALLET_REQUIRED";
    public const string BankAccountNotFound = "Transfer.BANK_ACCOUNT_NOT_FOUND";
    public const string CryptoWalletNotFound = "Transfer.CRYPTO_WALLET_NOT_FOUND";
    public const string BankAccountMismatch = "Transfer.BANK_ACCOUNT_MISMATCH";
    public const string CryptoWalletMismatch = "Transfer.CRYPTO_WALLET_MISMATCH";
    public const string SourceAmountInvalid = "Transfer.SOURCE_AMOUNT_INVALID";
    public const string DestinationRequired = "Transfer.DESTINATION_REQUIRED";
    public const string SourceRequired = "Transfer.SOURCE_REQUIRED";
    public const string SourceDestinationRequired = "Transfer.SOURCE_DESTINATION_REQUIRED";
    public const string ParticipantAccountRequired = "Transfer.PARTICIPANT_ACCOUNT_REQUIRED";
    public const string ParticipantDestinationRequired = "Transfer.PARTICIPANT_DESTINATION_REQUIRED";
    public const string ParticipantAccountNotFound = "Transfer.PARTICIPANT_ACCOUNT_NOT_FOUND";
    public const string OnrampingMethodRequired = "Transfer.ONRAMPING_METHOD_REQUIRED";
    public const string OnrampingMethodInvalid = "Transfer.ONRAMPING_METHOD_INVALID";
    public const string ProofRequired = "Transfer.PROOF_REQUIRED";
    public const string ProofTransactionIdTooLong = "Transfer.PROOF_TRANSACTION_ID_TOO_LONG";
    public const string ProofAuthenticationCodeTooLong = "Transfer.PROOF_AUTHENTICATION_CODE_TOO_LONG";
    public const string BalanceIdRequired = "Transfer.BALANCE_ID_REQUIRED";
    public const string BalanceNotFound = "Transfer.BALANCE_NOT_FOUND";
    public const string BalanceInsufficient = "Transfer.BALANCE_INSUFFICIENT";
    public const string ProducedAmountRequired = "Transfer.PRODUCED_AMOUNT_REQUIRED";
    public const string ProducedAssetRequired = "Transfer.PRODUCED_ASSET_REQUIRED";
    public const string ProducedChainRequired = "Transfer.PRODUCED_CHAIN_REQUIRED";
    public const string ProducedChainInvalid = "Transfer.PRODUCED_CHAIN_INVALID";
    public const string ProducedChainNamespaceMismatch = "Transfer.PRODUCED_CHAIN_NAMESPACE_MISMATCH";
    public const string AssetChainMismatch = "Transfer.ASSET_CHAIN_MISMATCH";
    public const string InvalidAggregateState = "Transfer.INVALID_AGGREGATE_STATE";
}
