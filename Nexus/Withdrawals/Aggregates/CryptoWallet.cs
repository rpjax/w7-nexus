using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Withdrawals.Errors;

namespace Nexus.Withdrawals.Aggregates;

public sealed class CryptoWallet
{
    public const int MaxAddressLength = 256;
    public const int MaxMemoLength = 128;
    public const int MaxLabelLength = 100;

    public string Id { get; }
    public string StrawManAccountId { get; }
    public Chain Chain { get; }
    public CryptoAsset Asset { get; }
    public string Address { get; private set; }
    public string? Memo { get; private set; }
    public string? Label { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    internal CryptoWallet(
        string Id,
        string StrawManAccountId,
        Chain Chain,
        CryptoAsset Asset,
        string Address,
        string? Memo,
        string? Label,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        this.Id = Id;
        this.StrawManAccountId = StrawManAccountId;
        this.Chain = Chain;
        this.Asset = Asset;
        this.Address = Address;
        this.Memo = Memo;
        this.Label = Label;
        this.CreatedAt = CreatedAt;
        this.UpdatedAt = UpdatedAt;
    }

    public static IResult<CryptoWallet> Create(
        string strawManAccountId,
        Chain chain,
        CryptoAsset asset,
        string address,
        string? memo,
        string? label)
    {
        var builder = Result.Create<CryptoWallet>();

        strawManAccountId = strawManAccountId?.Trim() ?? string.Empty;
        address = address?.Trim() ?? string.Empty;
        memo = string.IsNullOrWhiteSpace(memo) ? null : memo.Trim();
        label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();

        if (string.IsNullOrWhiteSpace(strawManAccountId))
            builder.WithError(Error.Create()
                .WithCode(CryptoWalletErrorCodes.StrawManInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        if (!Enum.IsDefined(chain))
            builder.WithError(Error.Create()
                .WithCode(CryptoWalletErrorCodes.ChainInvalid)
                .WithMessage("A rede blockchain informada é inválida.")
                .Build());

        if (!Enum.IsDefined(asset))
            builder.WithError(Error.Create()
                .WithCode(CryptoWalletErrorCodes.AssetInvalid)
                .WithMessage("O ativo crypto informado é inválido.")
                .Build());

        if (string.IsNullOrWhiteSpace(address))
            builder.WithError(Error.Create()
                .WithCode(CryptoWalletErrorCodes.AddressInvalid)
                .WithMessage("O endereço da wallet é obrigatório.")
                .Build());
        else if (address.Length > MaxAddressLength)
            builder.WithError(Error.Create()
                .WithCode(CryptoWalletErrorCodes.AddressTooLong)
                .WithMessage($"O endereço pode ter no máximo {MaxAddressLength} caracteres.")
                .Build());

        if (memo is not null && memo.Length > MaxMemoLength)
            builder.WithError(Error.Create()
                .WithCode(CryptoWalletErrorCodes.MemoTooLong)
                .WithMessage($"O memo pode ter no máximo {MaxMemoLength} caracteres.")
                .Build());

        if (label is not null && label.Length > MaxLabelLength)
            builder.WithError(Error.Create()
                .WithCode(CryptoWalletErrorCodes.LabelTooLong)
                .WithMessage($"O rótulo pode ter no máximo {MaxLabelLength} caracteres.")
                .Build());

        if (builder.ContainsError)
            return builder.Build();

        var now = DateTime.UtcNow;
        return builder.WithValue(new CryptoWallet(
            Id: string.Empty,
            StrawManAccountId: strawManAccountId,
            Chain: chain,
            Asset: asset,
            Address: address,
            Memo: memo,
            Label: label,
            CreatedAt: now,
            UpdatedAt: now)).Build();
    }

    public IResult UpdateLabel(string? label)
    {
        label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();

        if (label is not null && label.Length > MaxLabelLength)
            return Result.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.LabelTooLong)
                .WithMessage($"O rótulo pode ter no máximo {MaxLabelLength} caracteres.")
                .Build());

        Label = label;
        Touch();
        return Result.Success();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
