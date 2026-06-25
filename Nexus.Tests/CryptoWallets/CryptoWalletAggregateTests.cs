using Nexus.CryptoWallets.Aggregates;
using Xunit;

namespace Nexus.Tests.CryptoWallets;

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
    }

    [Fact]
    public void CryptoBalance_Create_AllowsDifferentChainAsset()
    {
        var origin = CryptoBalanceOrigin.Create("op-1", null).Value!;
        var split = CryptoBalanceSplit.Create("straw-1", 100m, 100m, CryptoSplitKind.ProfitShare).Value!;

        var usdtBalance = CryptoBalance.Create(
            "wallet-1",
            Chain.Polygon,
            CryptoAsset.Usdt,
            100m,
            "t-1",
            new[] { split },
            origin).Value!;
        var ethBalance = CryptoBalance.Create(
            "wallet-1",
            Chain.Ethereum,
            CryptoAsset.Eth,
            0.05m,
            "t-2",
            new[] { split },
            origin).Value!;

        Assert.Equal(CryptoAsset.Usdt, usdtBalance.Asset);
        Assert.Equal(Chain.Polygon, usdtBalance.Chain);
        Assert.Equal(CryptoAsset.Eth, ethBalance.Asset);
        Assert.Equal(Chain.Ethereum, ethBalance.Chain);
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
