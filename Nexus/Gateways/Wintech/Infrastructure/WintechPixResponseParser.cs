using System.Text.Json;
using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Legacy.Payments.Aggregates;

namespace Nexus.Gateways.Wintech.Infrastructure;

internal static class WintechPixResponseParser
{
    /// <summary>
    /// Plataforma whitelabel (mesmo modelo que SigiloPay): PK da transação = <c>transaction.id</c> no webhook;
    /// na criação PIX costuma vir como <c>transaction.id</c> aninhado ou <c>transactionId</c> na raiz.
    /// Persistido em <see cref="Payment.GatewayTransactionId"/>.
    /// </summary>
    internal static WintechPixPaymentResult ParseCreatePixResult(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var body = root;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Object)
            body = data;

        var transactionId = ResolveGatewayTransactionPk(body);
        var pixCode = ResolvePixCode(body);

        if (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(pixCode))
        {
            throw new InvalidOperationException(
                $"Wintech response did not include gateway transaction id (transaction.id or transactionId) and pix.code. Body: {json}");
        }

        return new WintechPixPaymentResult
        {
            TransactionId = transactionId,
            PixCode = pixCode
        };
    }

    private static string? ResolveGatewayTransactionPk(JsonElement root)
    {
        if (root.TryGetProperty("transaction", out var tx) && tx.ValueKind == JsonValueKind.Object &&
            tx.TryGetProperty("id", out var nestedId) && nestedId.ValueKind == JsonValueKind.String)
        {
            var s = nestedId.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                return s;
        }

        if (root.TryGetProperty("transactionId", out var txId) && txId.ValueKind == JsonValueKind.String)
        {
            var s = txId.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                return s;
        }

        return null;
    }

    private static string? ResolvePixCode(JsonElement root)
    {
        if (root.TryGetProperty("pix", out var pix) && pix.ValueKind == JsonValueKind.Object)
        {
            var code = ReadPixCode(pix);
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
        return null;
    }
}
