using ServicesApi.FusionPay.Client;
using ServicesApi.Payments;
using ServicesApi.Payments.Services;

namespace ServicesApi.FusionPay.Services;

public class FusionPayService : IPaymentGatewayService, IDisposable
{
    private FusionPayClient _client { get; }

    public FusionPayService(FusionPayClient client)
    {
        _client = client;
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public async Task<PixPayment> CreatePixPaymentAsync(
        string userId,
        decimal amount)
    {
        // Converte decimal para centavos (int)
        int amountInCents = (int)(amount * 100);

        var metadata = new Dictionary<string, string>()
        {
            { FusionPayConstants.UserIdMetadataKey, userId }
        };

        var request = new CreatePixPaymentRequest(
            amountInCents: amountInCents,
            paymentMethod: "pix",
            webhookUrl: "https://dickhouse.com/api/callback",
            customer: GenerateFakeCustomer(),
            items: [new Item("Digital Product", amountInCents.ToString(), 1, false)],
            metadata: metadata
        );

        var response = await _client.CreatePixPaymentAsync(request);

        if (!response.IsSuccess)
        {
            throw new Exception("Could not generate payment using FusionPay API.");
        }

        return new PixPayment(
            id: Guid.NewGuid().ToString(),
            pixCode: response.Data.Pix.QrCode);
    }

    #region Helpers
    private static Customer GenerateFakeCustomer() => new(
        name: "User_" + Guid.NewGuid().ToString()[..8],
        email: $"user_{Random.Shared.Next(1000)}@example.com",
        document: new Document("cpf", Mocker.GenerateFakeCpf()),
        phone: Mocker.GenerateFakePhone()
    );


    #endregion
}
