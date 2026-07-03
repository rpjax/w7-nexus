namespace Nexus.Composition;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Permissive cross-origin headers on every response, including Chrome Local Network
/// Access identity headers required for public sites (e.g. olx.com.br) to reach loopback.
/// </summary>
public sealed class OpenNetworkAccessMiddleware(
    RequestDelegate next,
    IConfiguration configuration)
{
    private readonly string _networkAccessName =
        configuration["Monkeypatches:NetworkAccessName"] ?? CrossOriginHeaders.DefaultNetworkAccessName;

    private readonly string _networkAccessId =
        configuration["Monkeypatches:NetworkAccessId"] ?? CrossOriginHeaders.DefaultNetworkAccessId;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            CrossOriginHeaders.Apply(context, _networkAccessName, _networkAccessId);
            return Task.CompletedTask;
        });

        if (HttpMethods.IsOptions(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        await next(context);
    }
}

public static class OpenNetworkAccessMiddlewareExtensions
{
    public static IApplicationBuilder UseOpenNetworkAccess(this IApplicationBuilder app) =>
        app.UseMiddleware<OpenNetworkAccessMiddleware>();
}
