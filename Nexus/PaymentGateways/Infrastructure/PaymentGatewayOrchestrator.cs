using Nexus.PaymentGateways.Application;
using Nexus.PaymentGateways.Application.Models;

namespace Nexus.PaymentGateways.Services;

public class PaymentGatewayOrchestrator : IPaymentGatewayOrchestrator
{
    private readonly IPaymentGatewayService[] _services;
    private readonly Func<int, int> _pickServiceIndex;

    /// <param name="services">Gateway implementations to try in random order.</param>
    /// <param name="pickServiceIndex">
    /// Given the current number of available gateways, returns a zero-based index to try next.
    /// Defaults to uniform random. Override in tests for deterministic ordering.
    /// </param>
    public PaymentGatewayOrchestrator(
        IEnumerable<IPaymentGatewayService> services,
        Func<int, int>? pickServiceIndex = null)
    {
        var servicesArray = services?.ToArray();

        if (servicesArray == null || servicesArray.Length == 0)
        {
            throw new ArgumentException("No payment services were provided. At least one service is required.");
        }

        _services = servicesArray;
        _pickServiceIndex = pickServiceIndex ?? (count => Random.Shared.Next(count));
    }

    public async Task<PixPayment> CreatePixPaymentAsync(CreateGatewayPixPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var availableServices = _services.ToList();

        while (availableServices.Count > 0)
        {
            var service = PickService(availableServices, _pickServiceIndex);

            try
            {
                return await service.CreatePixPaymentAsync(request);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                availableServices.Remove(service);
            }
        }

        throw new Exception("All available payment services failed to process the request.");
    }

    private static IPaymentGatewayService PickService(
        IList<IPaymentGatewayService> available,
        Func<int, int> pickServiceIndex)
    {
        var count = available.Count;
        var index = pickServiceIndex(count);
        if (index < 0 || index >= count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pickServiceIndex),
                $"Pick function returned index {index} for count {count}.");
        }

        return available[index];
    }
}

