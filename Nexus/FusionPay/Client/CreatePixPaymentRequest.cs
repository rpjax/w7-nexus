using System.Text.Json.Serialization;

namespace ServicesApi.FusionPay.Client;

public class CreatePixPaymentRequest
{
    [JsonPropertyName("amount")]
    public int AmountInCents { get; }

    [JsonPropertyName("payment_method")]
    public string PaymentMethod { get; } = "pix";

    [JsonPropertyName("postback_url")]
    public string WebhookUrl { get; } = "https://dickhouse.com/api/callback";

    [JsonPropertyName("customer")]
    public Customer Customer { get; }

    [JsonPropertyName("items")]
    public List<Item> Items { get; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; }

    public CreatePixPaymentRequest(
        int amountInCents, 
        string paymentMethod, 
        string webhookUrl, 
        Customer customer, 
        List<Item> items, 
        Dictionary<string, string> metadata)
    {
        AmountInCents = amountInCents;
        PaymentMethod = paymentMethod;
        WebhookUrl = webhookUrl;
        Customer = customer;
        Items = items;
        Metadata = metadata;
    }
}

public class Document
{
    [JsonPropertyName("type")]
    public string Type { get; }

    [JsonPropertyName("number")]
    public string Number { get; }

    public Document(string type, string number)
    {
        Type = type;
        Number = number;
    }
}

public class Customer
{
    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("email")]
    public string Email { get; }

    [JsonPropertyName("document")]
    public Document Document { get; }

    [JsonPropertyName("phone")]
    public string Phone { get; }

    public Customer(string name, string email, Document document, string phone)
    {
        Name = name;
        Email = email;
        Document = document;
        Phone = phone;
    }
}

public class Item
{
    [JsonPropertyName("title")]
    public string Title { get; }

    [JsonPropertyName("unit_price")]
    public string UnitPrice { get; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; }

    [JsonPropertyName("tangible")]
    public bool Tangible { get; }

    public Item(string title, string unitPrice, int quantity, bool tangible)
    {
        Title = title;
        UnitPrice = unitPrice;
        Quantity = quantity;
        Tangible = tangible;
    }
}

