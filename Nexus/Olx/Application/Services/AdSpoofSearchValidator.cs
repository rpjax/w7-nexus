using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Olx.Errors;

namespace Nexus.Olx.Application.Services;

internal static class AdSpoofSearchValidator
{
    public const int MaxKeywordLength = 200;

    public static IResult<(int Limit, int Offset, string? Keyword)> Validate(
        int limit,
        int offset,
        string? keyword)
    {
        var builder = Result.Create<(int Limit, int Offset, string? Keyword)>();

        var normalizedLimit = limit <= 0 ? 20 : limit;
        var normalizedKeyword = keyword?.Trim();

        if (normalizedLimit < 1 || normalizedLimit >= 1000)
        {
            builder.WithError(Error.Create()
                .WithCode(AdSpoofErrorCodes.SearchLimitInvalid)
                .WithMessage("O limite deve estar entre 1 e 999.")
                .Build());
        }

        if (offset < 0)
        {
            builder.WithError(Error.Create()
                .WithCode(AdSpoofErrorCodes.SearchOffsetInvalid)
                .WithMessage("O deslocamento não pode ser negativo.")
                .Build());
        }

        if (!string.IsNullOrWhiteSpace(normalizedKeyword) && normalizedKeyword.Length > MaxKeywordLength)
        {
            builder.WithError(Error.Create()
                .WithCode(AdSpoofErrorCodes.SearchKeywordTooLong)
                .WithMessage($"A palavra-chave pode ter no máximo {MaxKeywordLength} caracteres.")
                .Build());
        }

        if (builder.ContainsError)
            return builder.Build();

        return builder
            .WithValue((normalizedLimit, offset, normalizedKeyword))
            .Build();
    }

    public static IResult<T> RequestBodyRequiredResult<T>() =>
        Result<T>.Failure(Error.Create()
            .WithCode(AdSpoofErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisição é obrigatório.")
            .Build());

    public static string[] NormalizeFilterIds(string[]? ids) =>
        (ids ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
