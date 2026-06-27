using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Olx.Errors;

namespace Nexus.Olx.Aggregates;

public sealed class AdSpoof
{
    public string Id { get; }
    public string OperationId { get; }
    public string AdId { get; }
    public string AdUrl { get; private set; }
    public string? OperatorId { get; private set; }
    public bool IsImpersonating { get; private set; }
    public decimal? OriginalPrice { get; private set; }
    public decimal? PromotionalPrice { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    internal AdSpoof(
        string id,
        string operationId,
        string adId,
        string adUrl,
        string? operatorId,
        bool isImpersonating,
        decimal? originalPrice,
        decimal? promotionalPrice,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        OperationId = operationId;
        AdId = adId;
        AdUrl = adUrl;
        OperatorId = operatorId;
        IsImpersonating = isImpersonating;
        OriginalPrice = originalPrice;
        PromotionalPrice = promotionalPrice;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static IResult<AdSpoof> Create(string operationId, string adId, string adUrl)
    {
        operationId = operationId?.Trim() ?? string.Empty;
        adId = adId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(operationId))
            return Result<AdSpoof>.Failure(Error.Create()
                .WithCode(AdSpoofErrorCodes.OperationIdInvalid)
                .WithMessage("O ID da operação é obrigatório.")
                .Build());

        if (string.IsNullOrWhiteSpace(adId))
            return Result<AdSpoof>.Failure(Error.Create()
                .WithCode(AdSpoofErrorCodes.AdIdInvalid)
                .WithMessage("O ID do anúncio é obrigatório.")
                .Build());

        var urlValidation = TryNormalizeAdUrl(adUrl, out var normalizedAdUrl);
        if (urlValidation.IsFailure)
            return Result<AdSpoof>.Failure(urlValidation.Errors);

        var now = DateTime.UtcNow;
        return Result<AdSpoof>.Success(new AdSpoof(
            string.Empty,
            operationId,
            adId,
            normalizedAdUrl,
            operatorId: null,
            isImpersonating: false,
            originalPrice: null,
            promotionalPrice: null,
            createdAt: now,
            updatedAt: now));
    }

    public IResult EnsureAdUrl(string adUrl)
    {
        if (!string.IsNullOrWhiteSpace(AdUrl))
            return Result.Success();

        var urlValidation = TryNormalizeAdUrl(adUrl, out var normalizedAdUrl);
        if (urlValidation.IsFailure)
            return urlValidation;

        AdUrl = normalizedAdUrl;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public static IResult TryNormalizeAdUrl(string? adUrl, out string normalizedAdUrl)
    {
        normalizedAdUrl = adUrl?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedAdUrl))
            return Result.Failure(Error.Create()
                .WithCode(AdSpoofErrorCodes.AdUrlInvalid)
                .WithMessage("A URL do anúncio é obrigatória.")
                .Build());

        var candidate = normalizedAdUrl.Contains("://", StringComparison.Ordinal)
            ? normalizedAdUrl
            : $"https://{normalizedAdUrl}";

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            return Result.Failure(Error.Create()
                .WithCode(AdSpoofErrorCodes.AdUrlInvalid)
                .WithMessage("A URL do anúncio é inválida.")
                .Build());

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return Result.Failure(Error.Create()
                .WithCode(AdSpoofErrorCodes.AdUrlInvalid)
                .WithMessage("A URL do anúncio deve usar o protocolo HTTP ou HTTPS.")
                .Build());

        if (string.IsNullOrWhiteSpace(uri.Host))
            return Result.Failure(Error.Create()
                .WithCode(AdSpoofErrorCodes.AdUrlInvalid)
                .WithMessage("A URL do anúncio é inválida.")
                .Build());

        normalizedAdUrl = uri.GetLeftPart(UriPartial.Path);
        if (uri.Query.Length > 0)
            normalizedAdUrl += uri.Query;

        return Result.Success();
    }

    public IResult EnsureAvailableForOperator(string operatorId)
    {
        operatorId = operatorId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(operatorId))
            return Result.Failure(Error.Create()
                .WithCode(AdSpoofErrorCodes.OperatorIdInvalid)
                .WithMessage("O ID do operador é obrigatório.")
                .Build());

        if (!string.IsNullOrEmpty(OperatorId) && !string.Equals(OperatorId, operatorId, StringComparison.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(AdSpoofErrorCodes.AdAlreadySpoofed)
                .WithMessage("Este anúncio já está sendo spoofado por outro operador.")
                .Build());

        return Result.Success();
    }

    public IResult EnsureImpersonatedBy(string operatorId)
    {
        operatorId = operatorId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(operatorId))
            return Result.Failure(Error.Create()
                .WithCode(AdSpoofErrorCodes.OperatorIdInvalid)
                .WithMessage("O ID do operador é obrigatório.")
                .Build());

        if (!IsImpersonating || !string.Equals(OperatorId, operatorId, StringComparison.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(AdSpoofErrorCodes.ImpersonationRequired)
                .WithMessage("É necessário impersonar o anúncio antes de alterar os preços.")
                .Build());

        return Result.Success();
    }

    public IResult Impersonate(string operatorId)
    {
        operatorId = operatorId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(operatorId))
            return Result.Failure(Error.Create()
                .WithCode(AdSpoofErrorCodes.OperatorIdInvalid)
                .WithMessage("O ID do operador é obrigatório.")
                .Build());

        var availability = EnsureAvailableForOperator(operatorId);
        if (availability.IsFailure)
            return availability;

        OperatorId = operatorId;
        IsImpersonating = true;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public IResult Unimpersonate()
    {
        OperatorId = null;
        IsImpersonating = false;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public IResult UpdatePriceSpoof(string operatorId, decimal? originalPrice, decimal? promotionalPrice)
    {
        var availability = EnsureAvailableForOperator(operatorId);
        if (availability.IsFailure)
            return availability;

        var impersonation = EnsureImpersonatedBy(operatorId);
        if (impersonation.IsFailure)
            return impersonation;

        if (originalPrice is null && promotionalPrice is null)
            return Result.Failure(Error.Create()
                .WithCode(AdSpoofErrorCodes.PriceSpoofRequired)
                .WithMessage("Informe ao menos um preço para o spoof.")
                .Build());

        if (originalPrice is < 0)
            return Result.Failure(Error.Create()
                .WithCode(AdSpoofErrorCodes.OriginalPriceInvalid)
                .WithMessage("O preço original não pode ser negativo.")
                .Build());

        if (promotionalPrice is < 0)
            return Result.Failure(Error.Create()
                .WithCode(AdSpoofErrorCodes.PromotionalPriceInvalid)
                .WithMessage("O preço promocional não pode ser negativo.")
                .Build());

        if (originalPrice is not null)
            OriginalPrice = originalPrice;

        if (promotionalPrice is not null)
            PromotionalPrice = promotionalPrice;

        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
