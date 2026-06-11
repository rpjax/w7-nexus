using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Infrastructure;
using Nexus.Gateways.Wintech.Application;
using Nexus.Gateways.Wintech.Application.Models;
using Xunit;

namespace Nexus.Tests.Gateways;

public sealed class WintechGatewayPixServiceTests
{
    [Fact]
    public async Task CreateGatewayPixAsync_WhenClientSucceeds_ReturnsGatewayPix()
    {
        var credentials = new WintechApiCredentials
        {
            Id = "1",
            Name = "c",
            PublicKey = "pub",
            SecretKey = "sec"
        };
        var client = new StubWintechClient(new WintechPixPaymentResult
        {
            TransactionId = "trx-1",
            PixCode = "pix-copia-cola"
        });
        var sut = new WintechGatewayPixService(client, credentials);

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
    public async Task CreateGatewayPixAsync_WhenKeysMissing_ThrowsInvalidOperationException()
    {
        var credentials = new WintechApiCredentials
        {
            Id = "1",
            Name = "c",
            PublicKey = "pub",
            SecretKey = ""
        };
        var client = new StubWintechClient(new WintechPixPaymentResult());
        var sut = new WintechGatewayPixService(client, credentials);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
            {
                PaymentId = "internal-1",
                OperationId = "op-1",
                Amount = 10m
            }));
    }

    private sealed class StubWintechClient : IWintechClient
    {
        private readonly WintechPixPaymentResult _result;

        public StubWintechClient(WintechPixPaymentResult result)
        {
            _result = result;
        }

        public Task<WintechPixPaymentResult> CreatePixPaymentAsync(
            string publicKey,
            string secretKey,
            WintechPixPaymentRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }
}
