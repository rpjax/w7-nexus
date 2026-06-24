using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.AccountNodes.Errors;

namespace Nexus.AccountNodes.Aggregates;

public sealed class CryptoWalletAddress
{
    public const int MaxAddressLength = 256;
    public const int MaxMemoLength = 128;

    public AddressNamespace Namespace { get; }
    public string Address { get; }
    public string? Memo { get; }

    internal CryptoWalletAddress(AddressNamespace Namespace, string Address, string? Memo)
    {
        this.Namespace = Namespace;
        this.Address = Address;
        this.Memo = Memo;
    }

    public static IResult<CryptoWalletAddress> Create(AddressNamespace addressNamespace, string address, string? memo)
    {
        address = address?.Trim() ?? string.Empty;
        memo = string.IsNullOrWhiteSpace(memo) ? null : memo.Trim();

        if (!Enum.IsDefined(addressNamespace))
            return Result<CryptoWalletAddress>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.NamespaceInvalid)
                .WithMessage("O namespace de endereço informado é inválido.")
                .Build());

        if (string.IsNullOrWhiteSpace(address))
            return Result<CryptoWalletAddress>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.AddressInvalid)
                .WithMessage("O endereço da wallet é obrigatório.")
                .Build());

        if (address.Length > MaxAddressLength)
            return Result<CryptoWalletAddress>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.AddressTooLong)
                .WithMessage($"O endereço pode ter no máximo {MaxAddressLength} caracteres.")
                .Build());

        if (memo is not null && memo.Length > MaxMemoLength)
            return Result<CryptoWalletAddress>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.MemoTooLong)
                .WithMessage($"O memo pode ter no máximo {MaxMemoLength} caracteres.")
                .Build());

        return Result<CryptoWalletAddress>.Success(new CryptoWalletAddress(addressNamespace, address, memo));
    }
}

public sealed class CryptoWallet
{
    public const int MaxLabelLength = 100;

    public string Id { get; }
    public string StrawManId { get; }
    public string? Label { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<CryptoWalletAddress> _addresses;
    public IReadOnlyList<CryptoWalletAddress> Addresses => _addresses;

    private readonly List<CryptoBalance> _balances;
    public IReadOnlyList<CryptoBalance> Balances => _balances;

    internal CryptoWallet(
        string id,
        string strawManId,
        string? label,
        DateTime createdAt,
        DateTime updatedAt,
        IReadOnlyList<CryptoWalletAddress>? addresses = null,
        IReadOnlyList<CryptoBalance>? balances = null)
    {
        Id = id;
        StrawManId = strawManId;
        Label = label;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        _addresses = addresses?.ToList() ?? new List<CryptoWalletAddress>();
        _balances = balances?.ToList() ?? new List<CryptoBalance>();
    }

    public static IResult<CryptoWallet> Create(
        string strawManId,
        IReadOnlyList<CryptoWalletAddress> addresses,
        string? label)
    {
        var builder = Result.Create<CryptoWallet>();

        strawManId = strawManId?.Trim() ?? string.Empty;
        label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        addresses = addresses ?? Array.Empty<CryptoWalletAddress>();

        if (string.IsNullOrWhiteSpace(strawManId))
            builder.WithError(Error.Create()
                .WithCode(CryptoWalletErrorCodes.StrawManInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        if (addresses.Count == 0)
            builder.WithError(Error.Create()
                .WithCode(CryptoWalletErrorCodes.AddressRequired)
                .WithMessage("Informe ao menos um endereço por namespace.")
                .Build());

        if (addresses.Select(a => a.Namespace).Distinct().Count() != addresses.Count)
            builder.WithError(Error.Create()
                .WithCode(CryptoWalletErrorCodes.DuplicateNamespace)
                .WithMessage("Cada namespace pode ter apenas um endereço por wallet.")
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
            id: string.Empty,
            strawManId: strawManId,
            label: label,
            createdAt: now,
            updatedAt: now,
            addresses: addresses.ToList())).Build();
    }

    public bool HasAddressForNamespace(AddressNamespace addressNamespace) =>
        _addresses.Any(a => a.Namespace == addressNamespace);

    public CryptoWalletAddress? GetAddress(AddressNamespace addressNamespace) =>
        _addresses.FirstOrDefault(a => a.Namespace == addressNamespace);

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

    public IResult UpsertAddress(CryptoWalletAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        var index = _addresses.FindIndex(a => a.Namespace == address.Namespace);
        if (index < 0)
            _addresses.Add(address);
        else
            _addresses[index] = address;

        Touch();
        return Result.Success();
    }

    public IResult CreditBalance(CryptoBalance balance)
    {
        ArgumentNullException.ThrowIfNull(balance);
        _balances.Add(balance);
        Touch();
        return Result.Success();
    }

    public IResult<CryptoDebitPartialResult> DebitPartialBalance(string balanceId, decimal amount)
    {
        balanceId = balanceId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(balanceId))
            return Result<CryptoDebitPartialResult>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.BalanceIdInvalid)
                .WithMessage("O ID do saldo é obrigatório.")
                .Build());

        if (amount <= 0)
            return Result<CryptoDebitPartialResult>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.BalanceAmountInvalid)
                .WithMessage("O valor do débito deve ser maior que zero.")
                .Build());

        var index = _balances.FindIndex(b => b.Id == balanceId);
        if (index < 0)
            return Result<CryptoDebitPartialResult>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.BalanceNotFound)
                .WithMessage($"O saldo '{balanceId}' não foi encontrado.")
                .Build());

        var balance = _balances[index];

        if (amount > balance.Amount)
            return Result<CryptoDebitPartialResult>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.BalanceInsufficient)
                .WithMessage("O saldo é insuficiente para o débito solicitado.")
                .Build());

        CryptoBalance debitedBalance;
        CryptoBalance? remainderBalance = null;

        if (amount == balance.Amount)
        {
            _balances.RemoveAt(index);
            debitedBalance = balance;
        }
        else
        {
            var remainderAmount = balance.Amount - amount;
            remainderBalance = balance.WithAmount(remainderAmount);
            debitedBalance = balance.WithId(Guid.NewGuid().ToString("N")).WithAmount(amount);
            _balances[index] = remainderBalance;
        }

        Touch();
        return Result<CryptoDebitPartialResult>.Success(new CryptoDebitPartialResult(debitedBalance, remainderBalance));
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
