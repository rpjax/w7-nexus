using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Refactor.Nexus.Api.Authentication.Presentation.Http;

namespace Refactor.Nexus.Api.Tests;

public sealed class PublicSignupTests
{
    [Fact]
    public void Public_user_signup_use_case_is_absent()
    {
        var signupTypes = typeof(AuthenticationController).Assembly.GetTypes()
            .Where(type => type.Name.Contains("SignUpUser", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(signupTypes);
    }

    [Fact]
    public void Authentication_controller_has_no_public_user_signup_route()
    {
        var routes = typeof(AuthenticationController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpPostAttribute>())
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.DoesNotContain(routes, route =>
            route is not null && route.Contains("sign-up/usuario", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(routes, route =>
            route is not null && route.Equals("sign-up/admin", StringComparison.OrdinalIgnoreCase));
    }
}
