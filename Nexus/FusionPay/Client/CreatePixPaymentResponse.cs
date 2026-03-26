using System.Text.Json.Serialization;

namespace ServicesApi.FusionPay.Client;

public class CreatePixPaymentResponse
{
    [JsonPropertyName("success")]
    public bool IsSuccess { get; set; } 

    [JsonPropertyName("data")]
    public CreatePixPaymentResponseData Data { get; set; } = new();
}

public class CreatePixPaymentResponseData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("pix")]
    public CreatedPix Pix { get; set; } = new();
}

public class CreatedPix
{
    [JsonPropertyName("qr_code")]
    public string QrCode { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("e2_e")]
    public string E2e { get; set; } = string.Empty;
}

