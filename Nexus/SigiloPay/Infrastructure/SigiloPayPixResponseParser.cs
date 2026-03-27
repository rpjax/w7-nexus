using System.Text.Json;
using Nexus.SigiloPay.Application.Models;

namespace Nexus.SigiloPay.Infrastructure;

internal static class SigiloPayPixResponseParser
{
    internal static SigiloPayPixPaymentResult ParseCreatePixResult(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var transactionId = root.TryGetProperty("transactionId", out var txId) && txId.ValueKind == JsonValueKind.String
            ? txId.GetString()
            : null;

        string? pixCode = null;
        if (root.TryGetProperty("pix", out var pix) && pix.ValueKind == JsonValueKind.Object &&
            pix.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String)
        {
            pixCode = code.GetString();
        }

        if (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(pixCode))
        {
            throw new InvalidOperationException(
                $"SigiloPay response did not include transactionId and pix.code. Body: {json}");
        }

        return new SigiloPayPixPaymentResult
        {
            TransactionId = transactionId,
            PixCode = pixCode
        };
    }
}
