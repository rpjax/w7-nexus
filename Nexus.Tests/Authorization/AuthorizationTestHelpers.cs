using Nexus.Authorization.Application.Models;
using Xunit;

namespace Nexus.Tests.Authorization;

internal static class AuthorizationTestHelpers
{
    public static void AssertAuthorized(IAuthorizationResult result)
    {
        Assert.True(result.IsSuccess);
        Assert.True(result.IsAuthorized);
        Assert.Empty(result.AuthorizationErrors);
    }

    public static void AssertUnauthorized(IAuthorizationResult result, string expectedErrorCode)
    {
        Assert.True(result.IsSuccess);
        Assert.False(result.IsAuthorized);
        Assert.Contains(result.AuthorizationErrors, e => e.Code == expectedErrorCode);
    }

    public static void AssertPolicyFailure(IAuthorizationResult result, string expectedErrorCode)
    {
        Assert.True(result.IsFailure);
        Assert.False(result.IsAuthorized);
        Assert.Contains(result.Errors, e => e.Code == expectedErrorCode);
    }
}
