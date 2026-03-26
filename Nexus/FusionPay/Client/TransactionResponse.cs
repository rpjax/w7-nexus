using System.Text.Json.Serialization;

namespace ServicesApi.FusionPay.Client;

public class TransactionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    // Criado como string para tratar o formato BR dd/MM/yyyy manualmente ou via Converter
    [JsonPropertyName("created_at")]
    public string CreatedAtRaw { get; set; } = string.Empty;

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("company_id")]
    public string CompanyId { get; set; } = string.Empty;

    [JsonPropertyName("external_id")]
    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("paid_at")]
    public DateTime? PaidAt { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("refunded_amount")]
    public decimal? RefundedAmount { get; set; }

    [JsonPropertyName("payment_method")]
    public string PaymentMethod { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    // Metadata agora é um Dictionary, já que no JSON é um objeto {}
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    [JsonPropertyName("postback_url")]
    public string PostbackUrl { get; set; } = string.Empty;
}