using System.Text.Json.Serialization;

namespace ServicesApi.FusionPay.Client;

public class FusionPayResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; } = default!;

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}