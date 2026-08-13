using Refactor.Nexus.Api.Authorization;

namespace Refactor.Nexus.Api.Tests;

public sealed class RolesHygieneTests
{
    [Fact]
    public void Only_administrator_exists_and_is_grantable()
    {
        Assert.Equal("Administrator", Roles.Administrator);
        Assert.True(Roles.IsGrantable(Roles.Administrator));
        Assert.True(Roles.IsGrantable("administrator"));
        Assert.False(Roles.IsGrantable("Operator"));
        Assert.False(Roles.IsGrantable("StrawMan"));
        Assert.False(Roles.IsGrantable("OlxOperator"));
        Assert.False(Roles.IsGrantable("Gestor"));
        Assert.False(Roles.IsGrantable("Contador"));
        Assert.False(Roles.IsGrantable("Recrutador"));
    }
}
