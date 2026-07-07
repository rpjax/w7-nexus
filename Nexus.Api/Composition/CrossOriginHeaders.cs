namespace Nexus.Composition;

using Microsoft.AspNetCore.Http;

internal static class CrossOriginHeaders
{
    internal const string DefaultNetworkAccessName = "nexus-w7";
    internal const string DefaultNetworkAccessId = "7e:00:00:00:00:01";

    internal static void Apply(HttpContext context, string networkAccessName, string networkAccessId)
    {
        var origin = context.Request.Headers.Origin.FirstOrDefault();

        if (!string.IsNullOrEmpty(origin))
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            context.Response.Headers.Append("Vary", "Origin");
        }
        else
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        }

        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, HEAD, POST, PUT, PATCH, DELETE, OPTIONS";
        context.Response.Headers["Access-Control-Allow-Headers"] = "*";
        context.Response.Headers["Access-Control-Max-Age"] = "86400";
        context.Response.Headers["Access-Control-Allow-Private-Network"] = "true";
        context.Response.Headers["Private-Network-Access-Name"] = networkAccessName;
        context.Response.Headers["Private-Network-Access-ID"] = networkAccessId;
        context.Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
    }
}
