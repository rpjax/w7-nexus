using Aidan.Core.Linq.Extensions;
using Microsoft.AspNetCore.Mvc;
using Nexus.Frendz.Application;
using Nexus.Frendz.Application.Models;

namespace Nexus.Frendz.Presentation;

[ApiController]
[Route("api/frendz")]
public class FrendzController : ControllerBase
{
    private IFrendzApiKeysService _credentialsService { get; }
    private IFrendzApiCredentialsRepository _credentialsRepository { get; }

    public FrendzController(
        IFrendzApiKeysService credentialsService,
        IFrendzApiCredentialsRepository credentialsRepository)
    {
        _credentialsService = credentialsService;
        _credentialsRepository = credentialsRepository;
    }

    [HttpGet("credentials")]
    public async Task<IActionResult> GetCredentialsAsync()
    {
        var items = await _credentialsRepository.AsQueryable()
            .OrderBy(c => c.Name)
            .ToArrayAsync();
        var total = items.Length;

        return Ok(new
        {
            Total = total,
            Items = items,
        });
    }

    [HttpPost("credentials")]
    public async Task<IActionResult> AddCredentialsAsync([FromBody] AddCredentialsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest("Token is required.");

        var credentials = await _credentialsService.AddCredentialsAsync(
            null,
            request.Token,
            request.Name ?? string.Empty);
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
