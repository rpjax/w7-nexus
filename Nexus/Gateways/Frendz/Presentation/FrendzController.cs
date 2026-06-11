using Aidan.Core.Errors;
using Nexus.Gateways.Frendz.Application.Services.Contracts;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Aidan.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Gateways.Frendz.Application.Services;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.Frendz.Errors;
using Nexus.Legacy.Presentation;

namespace Nexus.Gateways.Frendz.Presentation;

[Route("api/frendz")]
public class FrendzController : WebController
{
    private IFrendzApiKeysService _credentialsService { get; }
    private IFrendzApiCredentialsRepository _credentialsRepository { get; }
    private IServiceScopeFactory _scopeFactory { get; }
    private ILogger<FrendzController> _logger { get; }

    public FrendzController(
        IFrendzApiKeysService credentialsService,
        IFrendzApiCredentialsRepository credentialsRepository,
        IServiceScopeFactory scopeFactory,
        ILogger<FrendzController> logger)
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
            (svc, json, ct) => svc.ProcessFrendzPostbackAsync(json, ct));

        return Ok();
    }

    [HttpPost("search")]
    public async Task<ActionResult> SearchCredentialsAsync([FromBody] SearchFrendzCredentialsRequest? request)
    {
        request ??= new SearchFrendzCredentialsRequest();

        var limit = request.Limit <= 0 ? 30 : request.Limit;
        var offset = request.Offset;
        var keyword = request.Keyword?.Trim();

        if (limit < 0 || limit >= 1000)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("Frendz.SEARCH_LIMIT_INVALID")
                .WithMessage("O limite deve estar entre 1 e 999.")
                .Build());
        }

        if (offset < 0)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("Frendz.SEARCH_OFFSET_INVALID")
                .WithMessage("O deslocamento não pode ser negativo.")
                .Build());
        }

        if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length > 200)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("Frendz.SEARCH_KEYWORD_TOO_LONG")
                .WithMessage("A palavra-chave pode ter no máximo 200 caracteres.")
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
                || c.Token.ToLower().Contains(term)
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
    public async Task<ActionResult> AddCredentialsAsync([FromBody] AddCredentialsRequest request)
    {
        if (request is null)
            return BadRequest("O corpo da requisição é obrigatório.");

        var result = await _credentialsService.AddCredentialsAsync(request);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(result.Value);
    }

    [HttpPut("credentials")]
    public async Task<ActionResult> UpdateCredentialsAsync([FromBody] UpdateCredentialsRequest request)
    {
        if (request is null)
            return BadRequest("O corpo da requisição é obrigatório.");

        var result = await _credentialsService.UpdateCredentialsAsync(request);
        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Code == FrendzErrorCodes.CredentialNotFound))
                return ProblemResponse(404, result.Errors);
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }

    [HttpPatch("credentials/enabled")]
    public async Task<ActionResult> SetCredentialEnabledAsync([FromBody] SetFrendzCredentialEnabledRequest request)
    {
        if (request is null)
            return BadRequest("O corpo da requisição é obrigatório.");

        var result = await _credentialsService.SetCredentialEnabledAsync(request);
        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Code == FrendzErrorCodes.CredentialNotFound))
                return ProblemResponse(404, result.Errors);
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }

    [HttpDelete("credentials")]
    public async Task<ActionResult> DeleteCredentialsAsync([FromQuery] string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("O parâmetro de consulta id é obrigatório.");

        var result = await _credentialsService.DeleteCredentialsAsync(id);
        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Code == FrendzErrorCodes.CredentialNotFound))
                return ProblemResponse(404, result.Errors);
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }
}
