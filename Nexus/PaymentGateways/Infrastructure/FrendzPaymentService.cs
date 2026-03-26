using Nexus.Frendz.Application;
using Nexus.Frendz.Infrastructure;
using Nexus.PaymentGateways.Application;
using Nexus.PaymentGateways.Application.Models;
using Nexus.Payments.Application;

namespace Nexus.PaymentGateways.Infrastructure;

public class FrendzPaymentService : IPaymentGatewayService
{
    private IFrendzApiKeysService _credentialsService { get; }
    private FrendzClient _frendzClient { get; }
    private IPixPaymentService _pixPaymentService { get; }

    public FrendzPaymentService(
        IFrendzApiKeysService credentialsService,
        FrendzClient frendzClient,
        IPixPaymentService pixPaymentService)
    {
        _credentialsService = credentialsService;
        _frendzClient = frendzClient;
        _pixPaymentService = pixPaymentService;
    }

    public async Task<PixPayment> CreatePixPaymentAsync(CreateGatewayPixPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var credentials = await _credentialsService.GetRandomCredentialsAsync();
        if (credentials is null || string.IsNullOrWhiteSpace(credentials.Token))
            throw new InvalidOperationException("No Frendz API credentials are available.");

        var amountCents = checked((int)Math.Round(request.Amount * 100m, MidpointRounding.AwayFromZero));
        var frendzResult = await _frendzClient.CreatePixPaymentAsync(
            credentials.Token,
            new FrendzPixPaymentRequest
            {
                AmountCents = amountCents,
                OfferHash = request.OfferHash,
                ProductHash = request.ProductHash,
                ProductTitle = request.ProductTitle,
                PostbackUrl = request.PostbackUrl,
                ExpireInDays = request.ExpireInDays,
                Customer = new FrendzCustomerInfo
                {
                    Name = request.CustomerName,
                    Email = request.CustomerEmail,
                    PhoneNumber = request.CustomerPhoneNumber,
                    Document = request.CustomerDocument
                }
            });

        var internalPaymentResult = await _pixPaymentService.CreatePixPaymentAsync(new CreatePixPaymentRequest
        {
            OperationId = request.OperationId,
            OperatorAccountId = request.OperatorAccountId,
            StrawManAccountId = request.StrawManAccountId,
            Gateway = Nexus.Payments.Aggregates.PaymentGateway.Frendz,
            Amount = request.Amount,
            GatewayPaymentId = frendzResult.TransactionId
        });

        if (internalPaymentResult.IsFailure || internalPaymentResult.Value is null)
            throw new InvalidOperationException("Failed to create internal PIX payment after Frendz transaction.");

        return new PixPayment
        {
            Id = internalPaymentResult.Value.Id,
            Code = frendzResult.PixCode
        };
    }

}
