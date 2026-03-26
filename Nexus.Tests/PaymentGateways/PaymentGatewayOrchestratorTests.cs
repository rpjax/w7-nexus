using Nexus.PaymentGateways.Application;
using Nexus.PaymentGateways.Application.Models;
using Nexus.PaymentGateways.Services;
using Xunit;

namespace Nexus.Tests.PaymentGateways;

public sealed class PaymentGatewayOrchestratorTests
{
    [Fact]
    public void Constructor_NullServices_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => new PaymentGatewayOrchestrator(null!));
        Assert.Contains("No payment services", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_EmptyServices_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new PaymentGatewayOrchestrator(Array.Empty<IPaymentGatewayService>()));
        Assert.Contains("No payment services", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatePixPaymentAsync_SingleServiceSucceeds_ReturnsPayment()
    {
        string? seenOperation = null;
        decimal seenAmount = 0;
        var stub = new StubPaymentGatewayService
        {
            OnCreate = request =>
            {
                seenOperation = request.OperationId;
                seenAmount = request.Amount;
                return Task.FromResult(new PixPayment { Id = "gw-1", Code = "pix-code" + request.Amount });
            }
        };
        var sut = new PaymentGatewayOrchestrator(new IPaymentGatewayService[] { stub }, _ => 0);

        var result = await sut.CreatePixPaymentAsync(new CreateGatewayPixPaymentRequest
        {
            OperationId = "op-1",
            Amount = 10m
        });

        Assert.Equal("op-1", seenOperation);
        Assert.Equal(10m, seenAmount);
        Assert.Equal("gw-1", result.Id);
        Assert.Equal("pix-code10", result.Code);
    }

    [Fact]
    public async Task CreatePixPaymentAsync_FirstFailsSecondSucceeds_WhenPickAlwaysZero_ReturnsSecond()
    {
        var failing = new StubPaymentGatewayService
        {
            OnCreate = _ => throw new InvalidOperationException("gateway down")
        };
        var succeeding = new StubPaymentGatewayService
        {
            OnCreate = _ => Task.FromResult(new PixPayment { Id = "ok", Code = "code" })
        };
        var sut = new PaymentGatewayOrchestrator(
            new IPaymentGatewayService[] { failing, succeeding },
            _ => 0);

        var result = await sut.CreatePixPaymentAsync(new CreateGatewayPixPaymentRequest { OperationId = "op-1", Amount = 1m });

        Assert.Equal("ok", result.Id);
        Assert.Equal("code", result.Code);
    }

    [Fact]
    public async Task CreatePixPaymentAsync_AllServicesFail_ThrowsWithAggregateMessage()
    {
        var stub = new StubPaymentGatewayService
        {
            OnCreate = _ => throw new TimeoutException("timeout")
        };
        var sut = new PaymentGatewayOrchestrator(
            new IPaymentGatewayService[] { stub, stub },
            _ => 0);

        var ex = await Assert.ThrowsAsync<Exception>(() => sut.CreatePixPaymentAsync(new CreateGatewayPixPaymentRequest { OperationId = "op-1", Amount = 1m }));
        Assert.Contains("All available payment services failed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatePixPaymentAsync_PickIndexOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        var stub = new StubPaymentGatewayService
        {
            OnCreate = _ => Task.FromResult(new PixPayment { Id = "x", Code = "y" })
        };
        var sut = new PaymentGatewayOrchestrator(
            new IPaymentGatewayService[] { stub },
            _ => 5);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.CreatePixPaymentAsync(new CreateGatewayPixPaymentRequest { OperationId = "op-1", Amount = 1m }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task CreatePixPaymentAsync_DeterministicPick_UsesCorrectGateway(int pickIndex)
    {
        var g0 = new StubPaymentGatewayService
        {
            OnCreate = _ => Task.FromResult(new PixPayment { Id = "g0", Code = "" })
        };
        var g1 = new StubPaymentGatewayService
        {
            OnCreate = _ => Task.FromResult(new PixPayment { Id = "g1", Code = "" })
        };
        var g2 = new StubPaymentGatewayService
        {
            OnCreate = _ => Task.FromResult(new PixPayment { Id = "g2", Code = "" })
        };
        var sut = new PaymentGatewayOrchestrator(
            new IPaymentGatewayService[] { g0, g1, g2 },
            count => pickIndex >= count ? count - 1 : pickIndex);

        var result = await sut.CreatePixPaymentAsync(new CreateGatewayPixPaymentRequest { OperationId = "op-1", Amount = 1m });

        Assert.Equal($"g{pickIndex}", result.Id);
    }
}
