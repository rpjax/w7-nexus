using Nexus.Charges.Application;
using Nexus.Charges.Application.Models;
using Nexus.Legacy.Wintech.Application;
using Nexus.Legacy.Wintech.Application.Models;

namespace Nexus.Charges.Infrastructure;

public sealed class WintechChargeService : IChargeService
{
    private IWintechClient _wintechClient { get; }
    private WintechApiCredentials _credentials { get; }

    public WintechChargeService(
        IWintechClient wintechClient,
        WintechApiCredentials credentials)
    {
        _wintechClient = wintechClient;
        _credentials = credentials;
    }

    public async Task<PixCharge> CreatePixChargeAsync(CreatePixChargeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.PaymentId))
            throw new InvalidOperationException("Payment id is required before calling gateway.");

        if (string.IsNullOrWhiteSpace(_credentials.PublicKey) || string.IsNullOrWhiteSpace(_credentials.SecretKey))
            throw new InvalidOperationException("Wintech public or secret key is missing for this credential.");

        var wintechResult = await _wintechClient.CreatePixPaymentAsync(
            _credentials.PublicKey,
            _credentials.SecretKey,
            new WintechPixPaymentRequest
            {
                Identifier = request.PaymentId,
                Amount = request.Amount,
                Client = GenerateProceduralCustomer()
            });

        return new PixCharge
        {
            Id = wintechResult.TransactionId,
            Code = wintechResult.PixCode,
        };
    }

    private static WintechCustomerInfo GenerateProceduralCustomer()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var ddd = Random.Shared.Next(11, 99);
        var mobileTail = Random.Shared.NextInt64(10000000, 99999999);
        return new WintechCustomerInfo
        {
            Name = $"Cliente {suffix}",
            Email = $"cliente.{suffix.ToLowerInvariant()}@mailinator.com",
            Phone = $"({ddd}) 9{mobileTail / 10000}-{mobileTail % 10000:D4}",
            Document = FormatCpf(GenerateValidCpfDigits())
        };
    }

    private static string FormatCpf(string digitsOnly11)
    {
        if (digitsOnly11.Length != 11)
            return digitsOnly11;
        return $"{digitsOnly11[..3]}.{digitsOnly11.Substring(3, 3)}.{digitsOnly11.Substring(6, 3)}-{digitsOnly11[^2..]}";
    }

    private static string GenerateValidCpfDigits()
    {
        Span<int> digits = stackalloc int[11];

        for (var i = 0; i < 9; i++)
            digits[i] = Random.Shared.Next(0, 10);
        if (digits[..9].ToArray().Distinct().Count() == 1)
            digits[8] = (digits[8] + 1) % 10;

        digits[9] = CalculateCpfVerifier(digits[..9], 10);
        digits[10] = CalculateCpfVerifier(digits[..10], 11);

        return string.Concat(digits.ToArray().Select(d => d.ToString()));
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
