using Nexus.Charges.Application.Models;
using Xunit;

namespace Nexus.Tests.Charges;

public sealed class PixChargeModelTests
{
    [Fact]
    public void DefaultInstance_HasEmptyStrings()
    {
        var p = new PixCharge();

        Assert.Equal(string.Empty, p.Id);
        Assert.Equal(string.Empty, p.Code);
        Assert.Null(p.GatewayTransactionId);
    }

    [Fact]
    public void CanSetFields()
    {
        var p = new PixCharge { Id = "tr-1", Code = "00020126...", GatewayTransactionId = "gw-99" };

        Assert.Equal("tr-1", p.Id);
        Assert.Equal("00020126...", p.Code);
        Assert.Equal("gw-99", p.GatewayTransactionId);
    }
}
