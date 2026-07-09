using Nexus.Scripts.Aggregates;
using Xunit;

namespace Nexus.Tests.Scripts;

public sealed class ChannelKeyTests
{
    [Theory]
    [InlineData(null, ChannelType.Production)]
    [InlineData("", ChannelType.Production)]
    [InlineData("prod", ChannelType.Production)]
    [InlineData("production", ChannelType.Production)]
    [InlineData("staging", ChannelType.Staging)]
    [InlineData("development", ChannelType.Development)]
    [InlineData("dev", ChannelType.Development)]
    public void Parse_DefaultAndStandardChannels(string? value, ChannelType expectedType)
    {
        var result = ChannelKey.Parse(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedType, result.Value!.Type);
    }

    [Fact]
    public void Parse_UnknownValueBecomesCustom()
    {
        var result = ChannelKey.Parse("someTest");

        Assert.True(result.IsSuccess);
        Assert.Equal(ChannelType.Custom, result.Value!.Type);
        Assert.Equal("someTest", result.Value.CustomName);
    }

    [Fact]
    public void Create_CustomWithoutName_Fails()
    {
        var result = ChannelKey.Create(ChannelType.Custom, null);

        Assert.True(result.IsFailure);
    }
}
