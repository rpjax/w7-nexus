using Aidan.Core.Errors;
using Nexus.Gateways.Wintech.Application.Services.Contracts;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Aidan.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Gateways.Wintech.Application.Services;
using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Gateways.Wintech.Errors;
using Nexus.Legacy.Presentation;
using Nexus.Payments.Aggregates;

namespace Nexus.Gateways.Wintech.Presentation;

[Route("api/wintech")]
public class WintechController : WebController
{
    private IWintechApiKeysService _credentialsService { get; }
    private IWintechApiCredentialsRepository _credentialsRepository { get; }
    private IServiceScopeFactory _scopeFactory { get; }
    private ILogger<WintechController> _logger { get; }

    public WintechController(
        IWintechApiKeysService credentialsService,
        IWintechApiCredentialsRepository credentialsRepository,
        IServiceScopeFactory scopeFactory,
        ILogger<WintechController> logger)
    {
        _credentialsService = credentialsService;
        _credentialsRepository = credentialsRepository;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpPost("webhook/callback")]
    public async Task<IActionResult> WebhookCallbackAsync(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(raw))
            raw = "{}";

        GatewayWebhookBackground.Enqueue(
            _scopeFactory,
            _logger,
            raw,
            (svc, json, ct) => svc.ProcessStandardGatewayWebhookAsync(PaymentGateway.Wintech, json, ct));

        return Ok();
    }

    [HttpPost("search")]
    public async Task<ActionResult> SearchCredentialsAsync([FromBody] SearchWintechCredentialsRequest? request)
    {
        request ??= new SearchWintechCredentialsRequest();

        var limit = request.Limit <= 0 ? 30 : request.Limit;
        var offset = request.Offset;
        var keyword = request.Keyword?.Trim();

        if (limit < 0 || limit >= 1000)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("Wintech.SEARCH_LIMIT_INVALID")
                .WithMessage("Limit must be between 1 and 999.")
                .Build());
        }

        if (offset < 0)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("Wintech.SEARCH_OFFSET_INVALID")
                .WithMessage("Offset cannot be negative.")
                .Build());
        }

        if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length > 200)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("Wintech.SEARCH_KEYWORD_TOO_LONG")
                .WithMessage("Keyword can have at most 200 characters.")
                .Build());
        }

        var query = _credentialsRepository.AsQueryable();

        if (request.EnabledOnly == true)
            query = query.Where(c => c.Enabled);
        else if (request.EnabledOnly == false)
            query = query.Where(c => !c.Enabled);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.ToLowerInvariant();
            query = query.Where(c =>
                c.Id.ToLower().Contains(term)
                || c.Name.ToLower().Contains(term)
                || c.PublicKey.ToLower().Contains(term)
                || c.SecretKey.ToLower().Contains(term)
                || (c.StrawManId != null && c.StrawManId.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.Name)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync();

        return Ok(new
        {
            Total = total,
            Items = items,
        });
    }

    [HttpPost("credentials")]
    public async Task<ActionResult> AddCredentialsAsync([FromBody] AddWintechCredentialsRequest request)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        var result = await _credentialsService.AddCredentialsAsync(request);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(result.Value);
    }

    [HttpPut("credentials")]
    public async Task<ActionResult> UpdateCredentialsAsync([FromBody] UpdateWintechCredentialsRequest request)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        var result = await _credentialsService.UpdateCredentialsAsync(request);
        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Code == WintechErrorCodes.CredentialNotFound))
                return ProblemResponse(404, result.Errors);
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }

    [HttpPatch("credentials/enabled")]
    public async Task<ActionResult> SetCredentialEnabledAsync([FromBody] SetWintechCredentialEnabledRequest request)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        var result = await _credentialsService.SetCredentialEnabledAsync(request);
        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Code == WintechErrorCodes.CredentialNotFound))
                return ProblemResponse(404, result.Errors);
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }

    [HttpDelete("credentials")]
    public async Task<ActionResult> DeleteCredentialsAsync([FromQuery] string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("Query parameter id is required.");

        var result = await _credentialsService.DeleteCredentialsAsync(id);
        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Code == WintechErrorCodes.CredentialNotFound))
                return ProblemResponse(404, result.Errors);
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }
}
