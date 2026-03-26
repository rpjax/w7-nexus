using Microsoft.AspNetCore.Mvc;
using Nexus.Frendz.Application;

namespace Nexus.Frendz.Presentation;

[ApiController]
[Route("api/frendz")]
public class FrendzController : ControllerBase
{
    private IFrendzApiKeysService _credentialsService { get; }

    public FrendzController(IFrendzApiKeysService credentialsService)
    {
        _credentialsService = credentialsService;
    }

    [HttpGet("credentials")]
    public async Task<IActionResult> GetCredentialsAsync()
    {
        var credentials = await _credentialsService.GetRandomCredentialsAsync();
        if (credentials is null)
            return NotFound();

        return Ok(credentials);
    }

    [HttpPost("credentials")]
    public async Task<IActionResult> AddCredentialsAsync([FromBody] AddCredentialsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest("Token is required.");

        var credentials = await _credentialsService.AddCredentialsAsync(request.Token, request.Name ?? string.Empty);
        return Ok(credentials);
    }

    [HttpDelete("credentials")]
    public async Task<IActionResult> DeleteCredentialsAsync([FromQuery] string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("Query parameter id is required.");

        var deleted = await _credentialsService.DeleteCredentialsAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}

public class AddCredentialsRequest
{
    public string Token { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
