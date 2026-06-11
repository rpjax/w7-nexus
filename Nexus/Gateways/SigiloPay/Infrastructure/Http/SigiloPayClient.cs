using System.Net.Http.Json;
using Nexus.Gateways.SigiloPay.Application.Services.Contracts;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nexus.Gateways.SigiloPay.Application.Models;

namespace Nexus.Gateways.SigiloPay.Infrastructure.Http;

/// <summary>
/// Cliente HTTP para a API SigiloPay v1. Autenticação: headers <c>x-public-key</c> e <c>x-secret-key</c>.
/// PIX: <c>POST /gateway/pix/receive</c>; <c>amount</c> em reais.
/// </summary>
public sealed class SigiloPayClient : ISigiloPayClient
{
    private const string ApiBaseUrl = "https://app.sigilopay.com.br/api/v1";

    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SigiloPayClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SigiloPayPixPaymentResult> CreatePixPaymentAsync(
        string publicKey,
        string secretKey,
        SigiloPayPixPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Identifier);
        ArgumentNullException.ThrowIfNull(request.Client);

        var uri = $"{ApiBaseUrl}/gateway/pix/receive";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri);
        httpRequest.Headers.TryAddWithoutValidation("x-public-key", publicKey);
        httpRequest.Headers.TryAddWithoutValidation("x-secret-key", secretKey);
        httpRequest.Content = JsonContent.Create(
            new ReceivePixRequestBody
            {
                Identifier = request.Identifier,
                Amount = request.Amount,
                Client = ClientPayload.From(request.Client)
            },
            options: JsonOptions);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"SigiloPay create PIX failed with {(int)response.StatusCode} {response.ReasonPhrase}: {responseText}");
        }

        return SigiloPayPixResponseParser.ParseCreatePixResult(responseText);
    }

    private sealed class ReceivePixRequestBody
    {
        public required string Identifier { get; init; }
        public required decimal Amount { get; init; }
        public required ClientPayload Client { get; init; }
    }

    private sealed class ClientPayload
    {
        public required string Name { get; init; }
        public required string Email { get; init; }
        public required string Phone { get; init; }
        public required string Document { get; init; }

        internal static ClientPayload From(SigiloPayCustomerInfo c) =>
            new()
            {
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                Document = c.Document
            };
    }
}
