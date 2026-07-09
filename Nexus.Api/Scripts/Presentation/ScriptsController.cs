using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nexus.Scripts.Application.Contracts;
using Nexus.Scripts.Application.Requests;

namespace Nexus.Scripts.Presentation;

[ApiController]
[Route("scripts")]
public sealed class ScriptsController : ControllerBase
{
    private readonly IScriptResolver _resolver;

    public ScriptsController(IScriptResolver resolver)
    {
        _resolver = resolver;
    }

    [HttpGet]
    public async Task<IActionResult> ResolveAsync(
        [FromQuery] string? host,
        [FromQuery] string? name,
        [FromQuery] string? channel,
        [FromQuery] bool allowDeprecated = false,
        [FromQuery] string? version = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _resolver.ResolveAsync(
            new ResolveScriptsRequest
            {
                Host = host,
                Name = name,
                Channel = channel,
                AllowDeprecated = allowDeprecated,
                Version = version,
            },
            cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(result.Errors);

        var response = result.Value!;
        var etag = $"\"{response.AggregateHash}\"";

        if (Request.Headers.IfNoneMatch.Any(value => string.Equals(value, etag, StringComparison.Ordinal)))
        {
            Response.Headers.ETag = etag;
            Response.Headers.CacheControl = "public, max-age=60";
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "public, max-age=60";
        return Ok(response);
    }
}
