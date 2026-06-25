using Nexus.CryptoWallets.Aggregates;

namespace Nexus.CryptoWallets.Presentation;

public static class CryptoWalletApiMapping
{
    public static object ToCryptoWalletResponse(CryptoWallet wallet, IReadOnlyList<CryptoBalance> balances)
    {
        var balancesByChainAsset = balances
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
            ownerId = wallet.OwnerId,
            addresses = wallet.Addresses.Select(ToWalletAddress).ToArray(),
            label = wallet.Label,
            balancesByChainAsset,
            balances = balances.Select(ToCryptoBalance).ToArray(),
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

    private static object ToCryptoBalance(CryptoBalance balance) => new
    {
        id = balance.Id,
        cryptoWalletId = balance.CryptoWalletId,
        chain = balance.Chain.ToString(),
        asset = balance.Asset.ToString(),
        amount = balance.Amount,
        transferId = balance.TransferId,
        createdAt = balance.CreatedAt,
        splits = balance.Splits,
        origin = ToBalanceOrigin(balance.Origin),
    };

    private static object ToBalanceOrigin(CryptoBalanceOrigin origin) => new
    {
        operationId = origin.OperationId,
        operatorId = origin.OperatorId,
    };
}
