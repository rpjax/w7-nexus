using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Errors;
using Xunit;

namespace Nexus.Tests.Withdrawals;

public sealed class CryptoWalletAggregateTests
{
    [Fact]
    public void Create_ValidWallet_Succeeds()
    {
        var result = CryptoWallet.Create(
            strawManAccountId: "straw-1",
            chain: Chain.Ethereum,
            asset: CryptoAsset.Usdt,
            address: "0xabc123",
            memo: null,
            label: "USDT ERC20");

        Assert.True(result.IsSuccess);
        Assert.Equal(Chain.Ethereum, result.Value!.Chain);
        Assert.Equal(CryptoAsset.Usdt, result.Value.Asset);
    }

    [Fact]
    public void Create_MissingAddress_Fails()
    {
        var result = CryptoWallet.Create(
            strawManAccountId: "straw-1",
            chain: Chain.Tron,
            asset: CryptoAsset.Usdt,
            address: "",
            memo: null,
            label: null);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == CryptoWalletErrorCodes.AddressInvalid);
    }
}
