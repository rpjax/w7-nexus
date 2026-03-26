using System.Text;
using System.Text.Json;
using ServicesApi.Configs;

namespace ServicesApi.FusionPay.Client;

public class FusionPayClient : IDisposable
{
    const string BASE_URL = "https://api.fusionpay.com.br";

    private string _apiKey { get; }
    private string _apiSecret { get; }

    private HttpClient _httpClient { get; }

    public FusionPayClient(FusionPayConfig configs)
    {
        _apiKey = configs.ApiKey;
        _apiSecret = configs.ApiSecret;
        _httpClient = new HttpClient();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private string GetAuthToken()
    {
        var raw = $"{_apiKey}:{_apiSecret}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public async Task<CreatePixPaymentResponse> CreatePixPaymentAsync(CreatePixPaymentRequest request)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}/v1/payment-transaction/create");
        requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", GetAuthToken());
        requestMessage.Content = JsonContent.Create(request);

        using var responseMessage = await _httpClient.SendAsync(requestMessage);

        if (!responseMessage.IsSuccessStatusCode)
        {
            var error = await responseMessage.Content.ReadAsStringAsync();
            throw new HttpRequestException($"FusionPay API Error: {responseMessage.StatusCode} - {error}");
        }

        var respJson = await responseMessage.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<CreatePixPaymentResponse>(respJson);

        return response ?? 
            throw new InvalidOperationException("Failed to deserialize FusionPay response.");
    }

    public async Task<TransactionResponse?> GetTransactionAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Transaction ID cannot be null or empty.", nameof(id));
        }

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{BASE_URL}/v1/payment-transaction/info/{id}");

        requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", GetAuthToken());

        using var responseMessage = await _httpClient.SendAsync(requestMessage);

        if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!responseMessage.IsSuccessStatusCode)
        {
            var error = await responseMessage.Content.ReadAsStringAsync();
            throw new HttpRequestException($"FusionPay API Error: {responseMessage.StatusCode} - {error}");
        }

        var respJson = await responseMessage.Content.ReadAsStringAsync();

        // O segredo está aqui: Deserializar para o Wrapper<TransactionResponse>
        var envelope = JsonSerializer.Deserialize<FusionPayResponse<TransactionResponse>>(respJson);
        var response = envelope?.Data;

        return response
            ?? throw new InvalidOperationException("Failed to deserialize FusionPay transaction response.");           
    }

}
