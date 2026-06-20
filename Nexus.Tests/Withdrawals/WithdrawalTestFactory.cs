using Nexus.Withdrawals.Aggregates;

namespace Nexus.Tests.Withdrawals;

internal static class WithdrawalTestFactory
{
    public static BankAccount CreateBankAccount(
        string? id = null,
        string strawManAccountId = "straw-1",
        BrazilianBank bank = BrazilianBank.BancodoBrasilSA_001,
        string agency = "1234",
        string accountNumber = "56789",
        string? accountDigit = "0",
        BankAccountType accountType = BankAccountType.Checking,
        string? pixKey = null,
        string? label = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null) =>
        new(
            id ?? Guid.NewGuid().ToString("N"),
            strawManAccountId,
            bank,
            agency,
            accountNumber,
            accountDigit,
            accountType,
            pixKey,
            label,
            createdAt ?? DateTime.UtcNow,
            updatedAt ?? DateTime.UtcNow);

    public static CryptoWallet CreateCryptoWallet(
        string? id = null,
        string strawManAccountId = "straw-1",
        Chain chain = Chain.Tron,
        CryptoAsset asset = CryptoAsset.Usdt,
        string address = "TXyz123456789",
        string? memo = null,
        string? label = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null) =>
        new(
            id ?? Guid.NewGuid().ToString("N"),
            strawManAccountId,
            chain,
            asset,
            address,
            memo,
            label,
            createdAt ?? DateTime.UtcNow,
            updatedAt ?? DateTime.UtcNow);
}
