namespace Nexus.Frendz.Infrastructure;

/// <summary>Dados do cliente exigidos pelo endpoint de transações (PIX).</summary>
public sealed class FrendzCustomerInfo
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
    public string? StreetName { get; init; }
    public string? Number { get; init; }
    public string? Complement { get; init; }
    public string? Neighborhood { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
}

/// <summary>Parâmetros para criar uma cobrança PIX via API Frendz (valores em centavos).</summary>
public sealed class FrendzPixPaymentRequest
{
    public int AmountCents { get; init; }
    public string OfferHash { get; init; } = string.Empty;
    public string ProductHash { get; init; } = string.Empty;
    public string ProductTitle { get; init; } = string.Empty;
    public FrendzCustomerInfo Customer { get; init; } = null!;
    public string? PostbackUrl { get; init; }
    public int ExpireInDays { get; init; } = 1;
}

/// <summary>Resultado da criação da transação PIX (identificador na Frendz + código copia-e-cola).</summary>
public sealed class FrendzPixPaymentResult
{
    public string TransactionId { get; init; } = string.Empty;
    public string PixCode { get; init; } = string.Empty;
}

internal sealed class FrendzPostTransactionBody
{
    public required int Amount { get; init; }
    public required string OfferHash { get; init; }
    public required string PaymentMethod { get; init; }
    public required FrendzCustomerJsonPayload Customer { get; init; }
    public required List<FrendzCartItemJsonPayload> Cart { get; init; }
    public int ExpireInDays { get; init; }
    public string TransactionOrigin { get; init; } = "api";
    public required FrendzTrackingJsonPayload Tracking { get; init; }
    public string? PostbackUrl { get; init; }

    internal static FrendzPostTransactionBody CreateForPix(FrendzPixPaymentRequest request)
    {
        return new FrendzPostTransactionBody
        {
            Amount = request.AmountCents,
            OfferHash = request.OfferHash,
            PaymentMethod = "pix",
            Customer = FrendzCustomerJsonPayload.From(request.Customer),
            Cart =
            [
                new FrendzCartItemJsonPayload
                {
                    ProductHash = request.ProductHash,
                    Title = request.ProductTitle,
                    Cover = null,
                    Price = request.AmountCents,
                    Quantity = 1,
                    OperationType = 1,
                    Tangible = false
                }
            ],
            ExpireInDays = request.ExpireInDays,
            TransactionOrigin = "api",
            Tracking = FrendzTrackingJsonPayload.Empty,
            PostbackUrl = request.PostbackUrl
        };
    }
}

internal sealed class FrendzCustomerJsonPayload
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string PhoneNumber { get; init; }
    public required string Document { get; init; }
    public string? StreetName { get; init; }
    public string? Number { get; init; }
    public string? Complement { get; init; }
    public string? Neighborhood { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }

    internal static FrendzCustomerJsonPayload From(FrendzCustomerInfo c)
    {
        return new FrendzCustomerJsonPayload
        {
            Name = c.Name,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            Document = c.Document,
            StreetName = c.StreetName,
            Number = c.Number,
            Complement = c.Complement,
            Neighborhood = c.Neighborhood,
            City = c.City,
            State = c.State,
            ZipCode = c.ZipCode
        };
    }
}

internal sealed class FrendzCartItemJsonPayload
{
    public required string ProductHash { get; init; }
    public required string Title { get; init; }
    public string? Cover { get; init; }
    public required int Price { get; init; }
    public int Quantity { get; init; } = 1;
    public int OperationType { get; init; } = 1;
    public bool Tangible { get; init; }
}

internal sealed class FrendzTrackingJsonPayload
{
    public string Src { get; init; } = "";
    public string UtmSource { get; init; } = "";
    public string UtmMedium { get; init; } = "";
    public string UtmCampaign { get; init; } = "";
    public string UtmTerm { get; init; } = "";
    public string UtmContent { get; init; } = "";

    internal static FrendzTrackingJsonPayload Empty { get; } = new();
}
