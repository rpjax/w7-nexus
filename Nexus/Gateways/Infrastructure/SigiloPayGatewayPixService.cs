using Nexus.Gateways.Application;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.SigiloPay.Application;
using Nexus.Gateways.SigiloPay.Application.Models;

namespace Nexus.Gateways.Infrastructure;

public sealed class SigiloPayGatewayPixService : IGatewayPixService
{
    private ISigiloPayClient _sigiloPayClient { get; }
    private SigiloPayApiCredentials _credentials { get; }

    public SigiloPayGatewayPixService(
        ISigiloPayClient sigiloPayClient,
        SigiloPayApiCredentials credentials)
    {
        _sigiloPayClient = sigiloPayClient;
        _credentials = credentials;
    }

    public async Task<GatewayPix> CreateGatewayPixAsync(CreateGatewayPixRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.PaymentId))
            throw new InvalidOperationException("Payment id is required before calling gateway.");

        if (string.IsNullOrWhiteSpace(_credentials.PublicKey) || string.IsNullOrWhiteSpace(_credentials.SecretKey))
            throw new InvalidOperationException("SigiloPay public or secret key is missing for this credential.");

        var sigiloResult = await _sigiloPayClient.CreatePixPaymentAsync(
            _credentials.PublicKey,
            _credentials.SecretKey,
            new SigiloPayPixPaymentRequest
            {
                Identifier = request.PaymentId,
                Amount = request.Amount,
                Client = GenerateProceduralCustomer()
            });

        return new GatewayPix
        {
            Id = sigiloResult.TransactionId,
            Code = sigiloResult.PixCode,
        };
    }

    private static SigiloPayCustomerInfo GenerateProceduralCustomer()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var ddd = Random.Shared.Next(11, 99);
        var mobileTail = Random.Shared.NextInt64(10000000, 99999999);
        return new SigiloPayCustomerInfo
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
