using System.Text.Json;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Payments.Aggregates;

namespace Nexus.Gateways.Frendz.Infrastructure;

internal static class FrendzPixResponseParser
{
    /// <summary>
    /// Resposta PIX oficial Frendz: <c>success</c> + <c>data.hash</c> (mesmo valor enviado no postback como
    /// <c>transaction_hash</c>) e <c>data.pix_code</c> (copia-e-cola). Persistido em
    /// <see cref="Payment.GatewayTransactionId"/>.
    /// </summary>
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
                $"Frendz response did not include data.hash (postback transaction_hash) and PIX copia-e-cola (data.pix_code or pix.code). Body: {json}");
        }

        return new FrendzPixPaymentResult
        {
            TransactionId = transactionId,
            PixCode = pixCode
        };
    }

    private static string? ResolveTransactionId(JsonElement root)
    {
        // Doc Frendz (PIX): "data.hash" na criação = "transaction_hash" no postback.
        if (root.TryGetProperty("hash", out var hash) && hash.ValueKind == JsonValueKind.String)
        {
            var h = hash.GetString();
            if (!string.IsNullOrWhiteSpace(h))
                return h;
        }

        if (root.TryGetProperty("transaction_hash", out var txHashSnake) && txHashSnake.ValueKind == JsonValueKind.String)
        {
            var s = txHashSnake.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                return s;
        }

        if (root.TryGetProperty("transactionHash", out var txHashCamel) && txHashCamel.ValueKind == JsonValueKind.String)
        {
            var s = txHashCamel.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                return s;
        }

        if (root.TryGetProperty("transaction", out var tx) && tx.ValueKind == JsonValueKind.String)
        {
            var s = tx.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                return s;
        }

        if (root.TryGetProperty("transaction", out var txObj) && txObj.ValueKind == JsonValueKind.Object)
        {
            if (txObj.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                return id.GetString();
            if (txObj.TryGetProperty("hash", out var txHash) && txHash.ValueKind == JsonValueKind.String)
                return txHash.GetString();
        }

        return null;
    }

    private static string? ResolvePixCode(JsonElement root)
    {
        // Doc Frendz (PIX): string EMV em "data.pix_code" (qr_code em data é PNG base64, não usar como copia-e-cola).
        if (root.TryGetProperty("pix_code", out var pixCode) && pixCode.ValueKind == JsonValueKind.String)
        {
            var s = pixCode.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                return s;
        }

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
        // Pagar.me–style payload under Frendz: EMV BR Code string.
        if (pix.TryGetProperty("pix_qr_code", out var pixQr) && pixQr.ValueKind == JsonValueKind.String)
            return pixQr.GetString();
        if (pix.TryGetProperty("qr_code", out var qr) && qr.ValueKind == JsonValueKind.String)
            return qr.GetString();
        return null;
    }
}
