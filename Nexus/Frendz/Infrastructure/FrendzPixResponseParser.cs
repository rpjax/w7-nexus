using System.Text.Json;

namespace Nexus.Frendz.Infrastructure;

internal static class FrendzPixResponseParser
{
    internal static FrendzPixPaymentResult ParseCreatePixResult(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Object)
        {
            root = data;
        }

        var transactionId = ResolveTransactionId(root);
        var pixCode = ResolvePixCode(root);

        if (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(pixCode))
        {
            throw new InvalidOperationException(
                $"Frendz response did not include transaction id and PIX code. Body: {json}");
        }

        return new FrendzPixPaymentResult
        {
            TransactionId = transactionId,
            PixCode = pixCode
        };
    }

    private static string? ResolveTransactionId(JsonElement root)
    {
        if (root.TryGetProperty("hash", out var hash) && hash.ValueKind == JsonValueKind.String)
            return hash.GetString();

        if (root.TryGetProperty("transaction", out var tx) && tx.ValueKind == JsonValueKind.Object)
        {
            if (tx.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                return id.GetString();
            if (tx.TryGetProperty("hash", out var txHash) && txHash.ValueKind == JsonValueKind.String)
                return txHash.GetString();
        }

        return null;
    }

    private static string? ResolvePixCode(JsonElement root)
    {
        if (root.TryGetProperty("pix", out var pixRoot) && pixRoot.ValueKind == JsonValueKind.Object)
        {
            var code = ReadPixCode(pixRoot);
            if (!string.IsNullOrWhiteSpace(code))
                return code;
        }

        if (root.TryGetProperty("transaction", out var tx) && tx.ValueKind == JsonValueKind.Object &&
            tx.TryGetProperty("pix", out var nestedPix) && nestedPix.ValueKind == JsonValueKind.Object)
        {
            return ReadPixCode(nestedPix);
        }

        return null;
    }

    private static string? ReadPixCode(JsonElement pix)
    {
        if (pix.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String)
            return code.GetString();
        if (pix.TryGetProperty("qr_code", out var qr) && qr.ValueKind == JsonValueKind.String)
            return qr.GetString();
        return null;
    }
}
