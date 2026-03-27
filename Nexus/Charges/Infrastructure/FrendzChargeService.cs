using Nexus.Charges.Application;
using Nexus.Charges.Application.Models;
using Nexus.Frendz.Application;
using Nexus.Frendz.Application.Models;
using Nexus.Frendz.Infrastructure;

namespace Nexus.Charges.Infrastructure;

public sealed class FrendzChargeService : IChargeService
{
    private IFrendzClient _frendzClient { get; }
    private FrendzApiCredentials _credentials { get; }

    public FrendzChargeService(
        IFrendzClient frendzClient,
        FrendzApiCredentials credentials)
    {
        _frendzClient = frendzClient;
        _credentials = credentials;
    }

    public async Task<PixCharge> CreatePixChargeAsync(CreatePixChargeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.PaymentId))
            throw new InvalidOperationException("Payment id is required before calling gateway.");

        if (string.IsNullOrWhiteSpace(_credentials.Token))
            throw new InvalidOperationException("Frendz API token is missing for this credential.");

        var amountCents = checked((int)Math.Round(request.Amount * 100m, MidpointRounding.AwayFromZero));
        var frendzResult = await _frendzClient.CreatePixPaymentAsync(
            _credentials.Token,
            new FrendzPixPaymentRequest
            {
                AmountCents = amountCents,
                OfferHash = request.PaymentId,
                ProductHash = $"prd_{request.PaymentId}_{Guid.NewGuid():N}",
                ProductTitle = "PIX Payment",
                ExpireInDays = 1,
                Customer = GenerateProceduralCustomer()
            });

        return new PixCharge
        {
            Id = frendzResult.TransactionId,
            Code = frendzResult.PixCode,
        };
    }

    private static FrendzCustomerInfo GenerateProceduralCustomer()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var ddd = Random.Shared.Next(11, 99);
        var mobileTail = Random.Shared.NextInt64(10000000, 99999999);
        return new FrendzCustomerInfo
        {
            Name = $"Cliente {suffix}",
            Email = $"cliente.{suffix.ToLowerInvariant()}@mailinator.com",
            PhoneNumber = $"{ddd}9{mobileTail}",
            Document = GenerateValidCpf()
        };
    }

    private static string GenerateValidCpf()
    {
        Span<int> digits = stackalloc int[11];

        for (var i = 0; i < 9; i++)
            digits[i] = Random.Shared.Next(0, 10);
        if (digits[..9].ToArray().Distinct().Count() == 1)
            digits[8] = (digits[8] + 1) % 10;

        digits[9] = CalculateCpfVerifier(digits[..9], 10);
        digits[10] = CalculateCpfVerifier(digits[..10], 11);

        return string.Concat(digits.ToArray());
    }

    private static int CalculateCpfVerifier(ReadOnlySpan<int> source, int initialWeight)
    {
        var sum = 0;
        for (var i = 0; i < source.Length; i++)
            sum += source[i] * (initialWeight - i);

        var mod = sum % 11;
        return mod < 2 ? 0 : 11 - mod;
    }
}
