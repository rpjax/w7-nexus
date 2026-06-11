using System.Net.Http.Json;
using Nexus.Gateways.Frendz.Application.Contracts;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nexus.Gateways.Frendz.Application;
using Nexus.Gateways.Frendz.Application.Models;

namespace Nexus.Gateways.Frendz.Application;

/// <summary>
/// Cliente HTTP para a API pública Frendz v1. Autenticação: query <c>api_token</c>.
/// PIX: <c>POST /transactions</c> com <c>payment_method=pix</c>; montantes em centavos.
/// </summary>
public sealed class FrendzClient : IFrendzClient
{
    private const string ApiUrl = "https://api.frendz.com.br/api/public/v1";

    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FrendzClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>Cria uma transação PIX na Frendz (POST /transactions).</summary>
    public async Task<FrendzPixPaymentResult> CreatePixPaymentAsync(
        string apiToken,
        FrendzPixPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiToken);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OfferHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProductHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProductTitle);
        ArgumentNullException.ThrowIfNull(request.Customer);

        var body = FrendzPostTransactionBody.CreateForPix(request);
        var uri = $"{ApiUrl}/transactions?api_token={Uri.EscapeDataString(apiToken)}";
        using var content = JsonContent.Create(body, options: JsonOptions);
        using var response = await _httpClient.PostAsync(uri, content, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Frendz create transaction failed with {(int)response.StatusCode} {response.ReasonPhrase}: {responseText}");
        }

        return FrendzPixResponseParser.ParseCreatePixResult(responseText);
    }
}
