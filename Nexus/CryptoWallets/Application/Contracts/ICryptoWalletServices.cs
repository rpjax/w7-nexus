using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Nexus.CryptoWallets.Aggregates;

namespace Nexus.CryptoWallets.Application.Contracts;

public interface ICryptoWalletRepository : IRepository<CryptoWallet>
{
    new Task<CryptoWallet> CreateAsync(CryptoWallet entity);
}

public interface ICryptoWalletService
{
    Task<IResult<CryptoWallet>> CreateAsync(CreateCryptoWalletRequest request);
    Task<IResult<CryptoWallet>> UpsertAddressAsync(UpsertCryptoWalletAddressRequest request);
    Task<IResult<CryptoWallet>> UpdateLabelAsync(string cryptoWalletId, string? label);
    Task<IResult<CryptoWallet>> GetByIdAsync(string cryptoWalletId);
}

public sealed class CreateCryptoWalletAddressRequest
{
    public AddressNamespace Namespace { get; init; }
    public string Address { get; init; } = string.Empty;
    public string? Memo { get; init; }
}

public sealed class CreateCryptoWalletRequest
{
    public string StrawManId { get; init; } = string.Empty;
    public IReadOnlyList<CreateCryptoWalletAddressRequest> Addresses { get; init; } = Array.Empty<CreateCryptoWalletAddressRequest>();
    public string? Label { get; init; }
}

public sealed class UpsertCryptoWalletAddressRequest
{
    public string CryptoWalletId { get; init; } = string.Empty;
    public AddressNamespace Namespace { get; init; }
    public string Address { get; init; } = string.Empty;
    public string? Memo { get; init; }
}

public sealed class UpsertCryptoWalletAddressBody
{
    public AddressNamespace Namespace { get; init; }
    public string Address { get; init; } = string.Empty;
    public string? Memo { get; init; }
}

public sealed class UpdateCryptoWalletLabelRequest
{
    public string? Label { get; init; }
}
