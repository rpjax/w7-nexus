using Nexus.Gateways.Application.Models;
using Xunit;

namespace Nexus.Tests.Gateways;

public sealed class GatewayPixModelTests
{
    [Fact]
    public void DefaultInstance_HasEmptyStrings()
    {
        var p = new GatewayPix();

        Assert.Equal(string.Empty, p.Id);
        Assert.Equal(string.Empty, p.Code);
    }

    [Fact]
    public void CanSetFields()
    {
        var p = new GatewayPix { Id = "tr-1", Code = "00020126..." };

        Assert.Equal("tr-1", p.Id);
        Assert.Equal("00020126...", p.Code);
    }
}
