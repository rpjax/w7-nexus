using Nexus.PaymentGateways.Application.Models;
using Xunit;

namespace Nexus.Tests.PaymentGateways;

public sealed class GatewayPixPaymentModelTests
{
    [Fact]
    public void DefaultInstance_HasEmptyStrings()
    {
        var p = new PixPayment();

        Assert.Equal(string.Empty, p.Id);
        Assert.Equal(string.Empty, p.Code);
    }

    [Fact]
    public void CanSetGatewayResponseFields()
    {
        var p = new PixPayment { Id = "tr-1", Code = "00020126..." };

        Assert.Equal("tr-1", p.Id);
        Assert.Equal("00020126...", p.Code);
    }
}
