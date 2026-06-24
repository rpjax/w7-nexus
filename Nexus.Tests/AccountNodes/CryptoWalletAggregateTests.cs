using Nexus.AccountNodes.Aggregates;
using Xunit;

namespace Nexus.Tests.AccountNodes;

public sealed class CryptoWalletAggregateTests
{
    [Fact]
    public void Create_WithNamespaceAddress_Succeeds()
    {
        var address = CryptoWalletAddress.Create(AddressNamespace.Tron, "TXyz123", null).Value!;
        var result = CryptoWallet.Create(
            "straw-1",
            new[] { address },
            "USDT wallet");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Addresses);
        Assert.Equal(AddressNamespace.Tron, result.Value.Addresses[0].Namespace);
        Assert.Equal("TXyz123", result.Value.Addresses[0].Address);
        Assert.Empty(result.Value.Balances);
    }

    [Fact]
    public void CreditBalance_AllowsDifferentChainAssetInSameWallet()
    {
        var address = CryptoWalletAddress.Create(AddressNamespace.Evm, "0xabc", null).Value!;
        var wallet = CryptoWallet.Create("straw-1", new[] { address }, null).Value!;
        var origin = BalanceOriginSnapshot.Create("op-1", null, "straw-1").Value!;
        var split = BalanceSplitSnapshot.Create("straw-1", 100m, 100m, SplitKind.ProfitShare).Value!;

        var usdtBalance = CryptoBalance.Create(
            Chain.Polygon, CryptoAsset.Usdt, 100m, "t-1", new[] { split }, Array.Empty<string>(), origin).Value!;
        var ethBalance = CryptoBalance.Create(
            Chain.Ethereum, CryptoAsset.Eth, 0.05m, "t-2", new[] { split }, Array.Empty<string>(), origin).Value!;

        Assert.True(wallet.CreditBalance(usdtBalance).IsSuccess);
        Assert.True(wallet.CreditBalance(ethBalance).IsSuccess);
        Assert.Equal(2, wallet.Balances.Count);
        Assert.Contains(wallet.Balances, b => b.Asset == CryptoAsset.Usdt && b.Chain == Chain.Polygon);
        Assert.Contains(wallet.Balances, b => b.Asset == CryptoAsset.Eth && b.Chain == Chain.Ethereum);
    }

    [Fact]
    public void UpsertAddress_ReplacesSameNamespace()
    {
        var tron = CryptoWalletAddress.Create(AddressNamespace.Tron, "T-old", null).Value!;
        var wallet = CryptoWallet.Create("straw-1", new[] { tron }, null).Value!;
        var updated = CryptoWalletAddress.Create(AddressNamespace.Tron, "T-new", "memo").Value!;

        Assert.True(wallet.UpsertAddress(updated).IsSuccess);
        Assert.Single(wallet.Addresses);
        Assert.Equal("T-new", wallet.Addresses[0].Address);
        Assert.Equal("memo", wallet.Addresses[0].Memo);
    }
}
