using Nexus.Frendz.Application;
using Nexus.Frendz.Infrastructure;
using Nexus.PaymentGateways.Application.Models;
using Nexus.PaymentGateways.Infrastructure;
using Nexus.Payments.Application;
using Nexus.Payments.Aggregates;
using Aidan.Core.Patterns;
using Xunit;

namespace Nexus.Tests.PaymentGateways;

public sealed class FrendzPaymentServiceTests
{
    [Fact]
    public async Task CreatePixPaymentAsync_WhenExternalAndInternalSucceed_ReturnsGatewayPayment()
    {
        var credentials = new StubFrendzApiKeysService();
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"hash\":\"trx-1\",\"pix\":{\"code\":\"pix-copia-cola\"}}")
        });
        var client = new FrendzClient(new HttpClient(handler));
        var pixPaymentService = new StubPixPaymentService
        {
            OnCreate = _ => Task.FromResult<Aidan.Core.Patterns.IResult<Nexus.Payments.Aggregates.PixPayment>>(
                Result.Create<Nexus.Payments.Aggregates.PixPayment>()
                    .WithValue(new Nexus.Payments.Aggregates.PixPayment(
                        "internal-1",
                        "op-1",
                        PaymentGateway.Frendz,
                        "trx-1",
                        10m))
                    .Build())
        };
        var sut = new FrendzPaymentService(credentials, client, pixPaymentService);

        var result = await sut.CreatePixPaymentAsync(new CreateGatewayPixPaymentRequest
        {
            OperationId = "op-1",
            Amount = 10m,
            OfferHash = "offer",
            ProductHash = "product",
            ProductTitle = "Produto",
            CustomerName = "User",
            CustomerEmail = "user@example.com",
            CustomerPhoneNumber = "11999999999",
            CustomerDocument = "12345678900"
        });

        Assert.Equal("internal-1", result.Id);
        Assert.Equal("pix-copia-cola", result.Code);
    }

    [Fact]
    public async Task CreatePixPaymentAsync_WhenNoCredentials_ThrowsInvalidOperationException()
    {
        var credentials = new StubFrendzApiKeysService { Credentials = null };
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        var client = new FrendzClient(new HttpClient(handler));
        var pixPaymentService = new StubPixPaymentService();
        var sut = new FrendzPaymentService(credentials, client, pixPaymentService);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreatePixPaymentAsync(new CreateGatewayPixPaymentRequest
            {
                OperationId = "op-1",
                Amount = 10m,
                OfferHash = "offer",
                ProductHash = "product",
                ProductTitle = "Produto",
                CustomerName = "User",
                CustomerEmail = "user@example.com",
                CustomerPhoneNumber = "11999999999",
                CustomerDocument = "12345678900"
            }));
    }

    private sealed class StubFrendzApiKeysService : IFrendzApiKeysService
    {
        public FredzApiCredentials? Credentials { get; set; } = new FredzApiCredentials { Token = "t" };

        public Task<FredzApiCredentials?> GetRandomCredentialsAsync() =>
            Task.FromResult(Credentials);

        public Task<FredzApiCredentials> AddCredentialsAsync(string token, string name) =>
            throw new NotImplementedException();

        public Task<bool> DeleteCredentialsAsync(string id) =>
            throw new NotImplementedException();
    }

    private sealed class StubPixPaymentService : IPixPaymentService
    {
        public Func<CreatePixPaymentRequest, Task<IResult<Nexus.Payments.Aggregates.PixPayment>>>? OnCreate { get; set; }

        public Task<Aidan.Core.Patterns.IResult<Nexus.Payments.Aggregates.PixPayment>> CreatePixPaymentAsync(CreatePixPaymentRequest request)
            => OnCreate is null
                ? Task.FromResult<Aidan.Core.Patterns.IResult<Nexus.Payments.Aggregates.PixPayment>>(
                    Result.Create<Nexus.Payments.Aggregates.PixPayment>()
                        .WithValue(new Nexus.Payments.Aggregates.PixPayment(
                            "internal-default",
                            request.OperationId ?? "op-1",
                            PaymentGateway.Frendz,
                            request.GatewayPaymentId ?? "trx-1",
                            request.Amount))
                        .Build())
                : OnCreate(request);

        public Task<Aidan.Core.Patterns.IResult> PayAsync(string paymentId) =>
            throw new NotImplementedException();

        public Task<Aidan.Core.Patterns.IResult> RefundAsync(string paymentId) =>
            throw new NotImplementedException();

        public Task<Aidan.Core.Patterns.IResult> KillAsync(string paymentId, string reason) =>
            throw new NotImplementedException();
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
