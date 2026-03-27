using Nexus.Charges.Application.Models;
using Nexus.Charges.Infrastructure;
using Nexus.Frendz.Application.Models;
using Nexus.Frendz.Infrastructure;
using Xunit;

namespace Nexus.Tests.Charges;

public sealed class FrendzChargeServiceTests
{
    [Fact]
    public async Task CreatePixChargeAsync_WhenHttpSucceeds_ReturnsPixCharge()
    {
        var credentials = new FrendzApiCredentials { Id = "1", Name = "c", Token = "t" };
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"hash\":\"trx-1\",\"pix\":{\"code\":\"pix-copia-cola\"}}")
        });
        var client = new FrendzClient(new HttpClient(handler));
        var sut = new FrendzChargeService(credentials, client);

        var result = await sut.CreatePixChargeAsync(new CreatePixChargeRequest
        {
            PaymentId = "internal-1",
            OperationId = "op-1",
            Amount = 10m
        });

        Assert.Equal("internal-1", result.Id);
        Assert.Equal("pix-copia-cola", result.Code);
        Assert.Equal("trx-1", result.GatewayTransactionId);
    }

    [Fact]
    public async Task CreatePixChargeAsync_WhenTokenMissing_ThrowsInvalidOperationException()
    {
        var credentials = new FrendzApiCredentials { Id = "1", Name = "c", Token = "" };
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        var client = new FrendzClient(new HttpClient(handler));
        var sut = new FrendzChargeService(credentials, client);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreatePixChargeAsync(new CreatePixChargeRequest
            {
                PaymentId = "internal-1",
                OperationId = "op-1",
                Amount = 10m
            }));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _onSend;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> onSend)
        {
            _onSend = onSend;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_onSend(request));
    }
}
