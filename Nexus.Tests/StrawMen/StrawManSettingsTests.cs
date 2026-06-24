using Nexus.StrawMen.Aggregates;
using Nexus.StrawMen.Application.Services;
using Xunit;

namespace Nexus.Tests.StrawMen;

public sealed class StrawManSettingsTests
{
    [Fact]
    public void Create_DefaultFee_IsValid()
    {
        var result = StrawManSettings.Create("straw-1", 0m, "admin-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value!.MovementFeePercentage);
    }

    [Fact]
    public void UpdateMovementFeePercentage_PersistsNewValue()
    {
        var settings = StrawManSettings.Create("straw-1", 0m, "admin-1").Value!;

        var update = settings.UpdateMovementFeePercentage(12.5m, "admin-2");

        Assert.True(update.IsSuccess);
        Assert.Equal(12.5m, settings.MovementFeePercentage);
        Assert.Equal("admin-2", settings.UpdatedByAdminId);
    }
}
