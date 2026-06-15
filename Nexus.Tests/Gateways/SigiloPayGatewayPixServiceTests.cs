using Nexus.Gateways.Application.Services;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Services;
using Nexus.Gateways.SigiloPay.Application.Models;
using Xunit;

namespace Nexus.Tests.Gateways;

public sealed class SigiloPayGatewayPixServiceTests
{
    [Fact]
    public async Task CreateGatewayPixAsync_WhenClientSucceeds_ReturnsGatewayPix()
    {
        var credentials = new SigiloPayApiCredentials
        {
            Id = "1",
            Name = "c",
            PublicKey = "pub",
            SecretKey = "sec"
        };
        var client = new StubSigiloPayClient(new SigiloPayPixPaymentResult
        {
            TransactionId = "trx-1",
            PixCode = "pix-copia-cola"
        });
        var sut = new SigiloPayGatewayPixService(client, credentials);

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
        var credentials = new SigiloPayApiCredentials
        {
            Id = "1",
            Name = "c",
            PublicKey = "",
            SecretKey = "sec"
        };
        var client = new StubSigiloPayClient(new SigiloPayPixPaymentResult());
        var sut = new SigiloPayGatewayPixService(client, credentials);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
            {
                PaymentId = "internal-1",
                OperationId = "op-1",
                Amount = 10m
            }));
    }

    private sealed class StubSigiloPayClient : ISigiloPayClient
    {
        private readonly SigiloPayPixPaymentResult _result;

        public StubSigiloPayClient(SigiloPayPixPaymentResult result)
        {
            _result = result;
        }

        public Task<SigiloPayPixPaymentResult> CreatePixPaymentAsync(
            string publicKey,
            string secretKey,
            SigiloPayPixPaymentRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }
}
