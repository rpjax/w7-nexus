using Nexus.Frendz.Application;
using Nexus.PaymentGateways.Infrastructure;
using Nexus.Payments.Application;
using Xunit;

namespace Nexus.Tests.PaymentGateways;

public sealed class FrendzPaymentServiceTests
{
    [Fact]
    public async Task CreatePixPaymentAsync_IsNotImplemented_Yet()
    {
        var credentials = new StubFrendzApiKeysService();
        var pixPaymentService = new StubPixPaymentService();
        var sut = new FrendzPaymentService(credentials, pixPaymentService);

        await Assert.ThrowsAsync<NotImplementedException>(() =>
            sut.CreatePixPaymentAsync("user-1", 15.50m));
    }

    private sealed class StubFrendzApiKeysService : IFrendzApiKeysService
    {
        public Task<FredzApiCredentials?> GetRandomCredentialsAsync() =>
            Task.FromResult<FredzApiCredentials?>(new FredzApiCredentials { Token = "t" });

        public Task<FredzApiCredentials> AddCredentialsAsync(string token, string name) =>
            throw new NotImplementedException();

        public Task<bool> DeleteCredentialsAsync(string id) =>
            throw new NotImplementedException();
    }

    private sealed class StubPixPaymentService : IPixPaymentService
    {
        public Task<Aidan.Core.Patterns.IResult<Nexus.Payments.Aggregates.PixPayment>> CreatePixPaymentAsync(CreatePixPaymentRequest request)
            => throw new NotImplementedException();

        public Task<Aidan.Core.Patterns.IResult> PayAsync(string paymentId) =>
            throw new NotImplementedException();

        public Task<Aidan.Core.Patterns.IResult> RefundAsync(string paymentId) =>
            throw new NotImplementedException();

        public Task<Aidan.Core.Patterns.IResult> KillAsync(string paymentId, string reason) =>
            throw new NotImplementedException();
    }
}
