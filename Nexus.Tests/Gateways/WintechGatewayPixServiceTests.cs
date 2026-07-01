using Nexus.Gateways.Application.Services;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Application.Requests;
using Nexus.Gateways.Application.Responses;
using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Gateways.Wintech.Application.Models;
using Xunit;

namespace Nexus.Tests.Gateways;

public sealed class WintechServiceTests
{
    [Fact]
    public async Task CreatePixAsync_WhenClientSucceeds_ReturnsCreatePixResponse()
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
        var sut = new WintechService(client, credentials);

        var result = await sut.CreatePixAsync(new CreatePixRequest
        {
            PaymentId = "internal-1",
            Amount = 10m
        });

        Assert.Equal("trx-1", result.TransactionId);
        Assert.Equal("pix-copia-cola", result.PixCode);
    }

    [Fact]
    public async Task CreatePixAsync_WhenKeysMissing_ThrowsInvalidOperationException()
    {
        var credentials = new WintechApiCredentials
        {
            Id = "1",
            Name = "c",
            PublicKey = "pub",
            SecretKey = ""
        };
        var client = new StubWintechClient(new WintechPixPaymentResult());
        var sut = new WintechService(client, credentials);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreatePixAsync(new CreatePixRequest
            {
                PaymentId = "internal-1",
                Amount = 10m
            }));
    }

    private sealed class StubWintechClient(WintechPixPaymentResult result) : IWintechClient
    {
        public Task<WintechPixPaymentResult> CreatePixPaymentAsync(
            string publicKey,
            string secretKey,
            WintechPixPaymentRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }
}
