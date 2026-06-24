using Nexus.AccountNodes.Aggregates;

namespace Nexus.AccountNodes.Presentation;

public static class AccountNodeApiMapping
{
    public static object ToBankAccountResponse(BankAccount account)
    {
        var totalBrl = account.Balances.Sum(balance => balance.AmountBrl);
        return new
        {
            id = account.Id,
            strawManId = account.StrawManId,
            bank = account.Bank.ToString(),
            agency = account.Agency,
            accountNumber = account.AccountNumber,
            accountDigit = account.AccountDigit,
            accountType = account.AccountType.ToString(),
            label = account.Label,
            totalBalanceBrl = totalBrl,
            balances = account.Balances.Select(ToBankBalance).ToArray(),
            createdAt = account.CreatedAt,
            updatedAt = account.UpdatedAt,
        };
    }

    public static object ToCryptoWalletResponse(CryptoWallet wallet)
    {
        var balancesByChainAsset = wallet.Balances
            .GroupBy(balance => new { balance.Chain, balance.Asset })
            .Select(group => new
            {
                chain = group.Key.Chain.ToString(),
                asset = group.Key.Asset.ToString(),
                totalAmount = group.Sum(balance => balance.Amount),
            })
            .ToArray();

        return new
        {
            id = wallet.Id,
            strawManId = wallet.StrawManId,
            addresses = wallet.Addresses.Select(ToWalletAddress).ToArray(),
            label = wallet.Label,
            balancesByChainAsset,
            balances = wallet.Balances.Select(ToCryptoBalance).ToArray(),
            createdAt = wallet.CreatedAt,
            updatedAt = wallet.UpdatedAt,
        };
    }

    private static object ToWalletAddress(CryptoWalletAddress address) => new
    {
        @namespace = address.Namespace.ToString(),
        address = address.Address,
        memo = address.Memo,
    };

    private static object ToBankBalance(BankBalance balance) => new
    {
        id = balance.Id,
        amountBrl = balance.AmountBrl,
        transferId = balance.TransferId,
        createdAt = balance.CreatedAt,
        splitSnapshot = balance.SplitSnapshot,
        appliedStrawManFeeIds = balance.AppliedStrawManFeeIds,
        originSnapshot = balance.OriginSnapshot,
    };

    private static object ToCryptoBalance(CryptoBalance balance) => new
    {
        id = balance.Id,
        chain = balance.Chain.ToString(),
        asset = balance.Asset.ToString(),
        amount = balance.Amount,
        transferId = balance.TransferId,
        createdAt = balance.CreatedAt,
        splitSnapshot = balance.SplitSnapshot,
        appliedStrawManFeeIds = balance.AppliedStrawManFeeIds,
        originSnapshot = balance.OriginSnapshot,
    };
}
