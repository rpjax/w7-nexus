using Nexus.Gateways.Application.Services;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Application.Requests;
using Nexus.Gateways.Application.Responses;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Models;
using Xunit;

namespace Nexus.Tests.Gateways;

public sealed class SigiloPayServiceTests
{
    [Fact]
    public async Task CreatePixAsync_WhenClientSucceeds_ReturnsCreatePixResponse()
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
        var sut = new SigiloPayService(client, credentials);

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
        var credentials = new SigiloPayApiCredentials
        {
            Id = "1",
            Name = "c",
            PublicKey = "",
            SecretKey = "sec"
        };
        var client = new StubSigiloPayClient(new SigiloPayPixPaymentResult());
        var sut = new SigiloPayService(client, credentials);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreatePixAsync(new CreatePixRequest
            {
                PaymentId = "internal-1",
                Amount = 10m
            }));
    }

    private sealed class StubSigiloPayClient(SigiloPayPixPaymentResult result) : ISigiloPayClient
    {
        public Task<SigiloPayPixPaymentResult> CreatePixPaymentAsync(
            string publicKey,
            string secretKey,
            SigiloPayPixPaymentRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }
}
