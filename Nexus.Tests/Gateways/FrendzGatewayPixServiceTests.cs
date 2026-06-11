using Nexus.AppHost;
using Nexus.AppHost.Contracts;
using Nexus.Gateways.Application;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Frendz.Application;
using Nexus.Gateways.Frendz.Application.Models;
using Xunit;

namespace Nexus.Tests.Gateways;

public sealed class FrendzGatewayPixServiceTests
{
    [Fact]
    public async Task CreateGatewayPixAsync_WhenHttpSucceeds_ReturnsGatewayPix()
    {
        var credentials = new FrendzApiCredentials { Id = "1", Name = "c", Token = "t" };
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"hash\":\"trx-1\",\"pix\":{\"code\":\"pix-copia-cola\"}}")
        });
        var client = new FrendzClient(new HttpClient(handler));
        var sut = new FrendzGatewayPixService(client, new StubAppHostProvider(), credentials);

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            PaymentId = "internal-1",
            OperationId = "op-1",
            Amount = 10m
        });

        Assert.Equal("trx-1", result.Id);
        Assert.Equal("pix-copia-cola", result.Code);
    }

    [Fact]
    public async Task CreateGatewayPixAsync_WhenOfficialPixResponse_UsesDataHashAndPixCode()
    {
        var credentials = new FrendzApiCredentials { Id = "1", Name = "c", Token = "t" };
        const string body =
            "{\"success\":true,\"data\":{\"hash\":\"trans123abc456\",\"status\":\"pending\",\"amount\":15000,\"payment_method\":\"pix\",\"qr_code\":\"data:image/png;base64,AAA\",\"pix_code\":\"00020126580014BR.GOV.BCB.PIX\",\"expires_at\":\"2025-01-20T15:30:00Z\",\"created_at\":\"2025-01-20T10:30:00Z\"}}";
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(body)
        });
        var client = new FrendzClient(new HttpClient(handler));
        var sut = new FrendzGatewayPixService(client, new StubAppHostProvider(), credentials);

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            PaymentId = "internal-1",
            OperationId = "op-1",
            Amount = 150m
        });

        Assert.Equal("trans123abc456", result.Id);
        Assert.Equal("00020126580014BR.GOV.BCB.PIX", result.Code);
    }

    [Fact]
    public async Task CreateGatewayPixAsync_WhenTokenMissing_ThrowsInvalidOperationException()
    {
        var credentials = new FrendzApiCredentials { Id = "1", Name = "c", Token = "" };
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        var client = new FrendzClient(new HttpClient(handler));
        var sut = new FrendzGatewayPixService(client, new StubAppHostProvider(), credentials);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
            {
                PaymentId = "internal-1",
                OperationId = "op-1",
                Amount = 10m
            }));
    }

    private sealed class StubAppHostProvider : IAppHostProvider
    {
        public string? BaseUrl => null;

        public string GetWebhookCallbackUrl(string gatewayApiSegment) =>
            throw new InvalidOperationException("Not used when BaseUrl is null.");
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
