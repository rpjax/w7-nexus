using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Payments.Application.Models;
using Nexus.Payments.Errors;

namespace Nexus.Payments.Application.Services;

internal static class PaymentSearchValidator
{
    private const int MaxKeywordLength = 200;

    public static IResult<(int Limit, int Offset, string? Keyword)> Validate(SearchPaymentsRequest? request)
    {
        request ??= new SearchPaymentsRequest();

        var builder = Result.Create<(int Limit, int Offset, string? Keyword)>();

        var limit = request.Limit <= 0 ? 30 : request.Limit;
        var offset = request.Offset;
        var keyword = request.Keyword?.Trim();

        if (limit < 1 || limit >= 1000)
        {
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.SearchLimitInvalid)
                .WithMessage("O limite deve estar entre 1 e 999.")
                .Build());
        }

        if (offset < 0)
        {
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.SearchOffsetInvalid)
                .WithMessage("O deslocamento não pode ser negativo.")
                .Build());
        }

        if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length > MaxKeywordLength)
        {
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.SearchKeywordTooLong)
                .WithMessage($"A palavra-chave pode ter no máximo {MaxKeywordLength} caracteres.")
                .Build());
        }

        if (builder.ContainsError)
            return builder.Build();

        return builder
            .WithValue((limit, offset, keyword))
            .Build();
    }

    public static IResult<T> RequestBodyRequiredResult<T>() =>
        Result<T>.Failure(Error.Create()
            .WithCode(PixPaymentErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisição é obrigatório.")
            .Build());
}
