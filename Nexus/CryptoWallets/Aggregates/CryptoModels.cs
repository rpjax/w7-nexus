namespace Nexus.CryptoWallets.Aggregates;

public enum AddressNamespace
{
    Evm = 1,
    Tron,
    Solana,
    Bitcoin,
    Litecoin,
    Starknet,
    Ton,
}

public enum CryptoAsset
{
    Usdt = 1,
    Usdc,
    Btc,
    Eth,
    Ltc,
}

public enum Chain
{
    Tron = 1,
    BnbSmartChain,
    Ethereum,
    Polygon,
    Solana,
    ArbitrumOne,
    Optimism,
    Base,
    AvalancheCChain,
    Bitcoin,
    ZkSyncEra,
    Linea,
    Scroll,
    Mantle,
    MantaPacific,
    Starknet,
    Ton,
    Litecoin,
}

public static class ChainExtensions
{
    public static string ToCaip2(this Chain chain) => chain switch
    {
        Chain.Tron => "tron:0x2b6653dc",
        Chain.BnbSmartChain => "eip155:56",
        Chain.Ethereum => "eip155:1",
        Chain.Polygon => "eip155:137",
        Chain.Solana => "solana:5eykt4UsFv8P8NJdTREpY1vzqKqZKvdp",
        Chain.ArbitrumOne => "eip155:42161",
        Chain.Optimism => "eip155:10",
        Chain.Base => "eip155:8453",
        Chain.AvalancheCChain => "eip155:43114",
        Chain.Bitcoin => "bip122:000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f",
        Chain.ZkSyncEra => "eip155:324",
        Chain.Linea => "eip155:59144",
        Chain.Scroll => "eip155:534352",
        Chain.Mantle => "eip155:5000",
        Chain.MantaPacific => "eip155:169",
        Chain.Starknet => "starknet:SN_MAIN",
        Chain.Ton => "ton:mainnet",
        Chain.Litecoin => "bip122:12a765e31ffd4059bada1e25190ca6b1",
        _ => throw new ArgumentOutOfRangeException(nameof(chain), chain, null),
    };

    public static AddressNamespace GetNamespace(this Chain chain) => chain switch
    {
        Chain.Tron => AddressNamespace.Tron,
        Chain.Solana => AddressNamespace.Solana,
        Chain.Bitcoin => AddressNamespace.Bitcoin,
        Chain.Litecoin => AddressNamespace.Litecoin,
        Chain.Starknet => AddressNamespace.Starknet,
        Chain.Ton => AddressNamespace.Ton,
        Chain.BnbSmartChain or Chain.Ethereum or Chain.Polygon or Chain.ArbitrumOne
            or Chain.Optimism or Chain.Base or Chain.AvalancheCChain or Chain.ZkSyncEra
            or Chain.Linea or Chain.Scroll or Chain.Mantle or Chain.MantaPacific => AddressNamespace.Evm,
        _ => throw new ArgumentOutOfRangeException(nameof(chain), chain, null),
    };

    public static bool IsEvm(this Chain chain) => chain.GetNamespace() == AddressNamespace.Evm;
}

public static class CryptoAssetExtensions
{
    public static bool IsSupportedOnChain(this CryptoAsset asset, Chain chain)
    {
        return asset switch
        {
            CryptoAsset.Btc => chain == Chain.Bitcoin,
            CryptoAsset.Ltc => chain == Chain.Litecoin,
            CryptoAsset.Eth => chain.IsEvm(),
            CryptoAsset.Usdt or CryptoAsset.Usdc => chain is not Chain.Bitcoin and not Chain.Litecoin,
            _ => false,
        };
    }
}
