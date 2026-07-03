using Microsoft.AspNetCore.Mvc;

namespace Nexus.Monkeypatches.Presentation;

[ApiController]
[Route("monkeypatches")]
public sealed class MonkeypatchesController(IWebHostEnvironment environment) : ControllerBase
{
    private static readonly Dictionary<string, string> OriginPatchRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["https://www.olx.com.br"] = "monkeypatches/patches/olx.min.js",
        ["https://olx.com.br"] = "monkeypatches/patches/olx.min.js",
    };

    [HttpGet]
    [Produces("application/javascript")]
    public IActionResult GetByOrigin([FromQuery] string? origin)
    {
        var relativePath = ResolvePatchPath(origin);
        if (relativePath is null)
        {
            return NotFound($"No patch registered for origin: {origin ?? "(missing)"}");
        }

        var filePath = Path.Combine(
            environment.WebRootPath,
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("Patch file not found");
        }

        Response.Headers.CacheControl = "no-cache";
        return PhysicalFile(filePath, "application/javascript; charset=utf-8");
    }

    private static string? ResolvePatchPath(string? origin)
    {
        if (string.IsNullOrEmpty(origin))
        {
            return null;
        }

        if (OriginPatchRoutes.TryGetValue(origin, out var exact))
        {
            return exact;
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return uri.Host.Contains("olx", StringComparison.OrdinalIgnoreCase)
            ? "monkeypatches/patches/olx.min.js"
            : null;
    }
}
