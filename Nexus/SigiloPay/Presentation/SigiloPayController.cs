using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Aidan.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexus.SigiloPay.Application;
using Nexus.SigiloPay.Application.Models;
using Nexus.SigiloPay.ErrorCodes;

namespace Nexus.SigiloPay.Presentation;

[Route("api/sigilopay")]
public class SigiloPayController : WebController
{
    private ISigiloPayApiKeysService _credentialsService { get; }
    private ISigiloPayApiCredentialsRepository _credentialsRepository { get; }

    public SigiloPayController(
        ISigiloPayApiKeysService credentialsService,
        ISigiloPayApiCredentialsRepository credentialsRepository)
    {
        _credentialsService = credentialsService;
        _credentialsRepository = credentialsRepository;
    }

    [HttpPost("search")]
    public async Task<ActionResult> SearchCredentialsAsync([FromBody] SearchSigiloPayCredentialsRequest? request)
    {
        request ??= new SearchSigiloPayCredentialsRequest();

        var limit = request.Limit <= 0 ? 30 : request.Limit;
        var offset = request.Offset;
        var keyword = request.Keyword?.Trim();

        if (limit < 0 || limit >= 1000)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("SigiloPay.SEARCH_LIMIT_INVALID")
                .WithMessage("Limit must be between 1 and 999.")
                .Build());
        }

        if (offset < 0)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("SigiloPay.SEARCH_OFFSET_INVALID")
                .WithMessage("Offset cannot be negative.")
                .Build());
        }

        if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length > 200)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("SigiloPay.SEARCH_KEYWORD_TOO_LONG")
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
    public async Task<ActionResult> AddCredentialsAsync([FromBody] AddSigiloPayCredentialsRequest request)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        var result = await _credentialsService.AddCredentialsAsync(request);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(result.Value);
    }

    [HttpPut("credentials")]
    public async Task<ActionResult> UpdateCredentialsAsync([FromBody] UpdateSigiloPayCredentialsRequest request)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        var result = await _credentialsService.UpdateCredentialsAsync(request);
        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Code == SigiloPayErrorCodes.CredentialNotFound))
                return ProblemResponse(404, result.Errors);
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }

    [HttpPatch("credentials/enabled")]
    public async Task<ActionResult> SetCredentialEnabledAsync([FromBody] SetSigiloPayCredentialEnabledRequest request)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        var result = await _credentialsService.SetCredentialEnabledAsync(request);
        if (result.IsFailure)
        {
            if (result.Errors.Any(e => e.Code == SigiloPayErrorCodes.CredentialNotFound))
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
            if (result.Errors.Any(e => e.Code == SigiloPayErrorCodes.CredentialNotFound))
                return ProblemResponse(404, result.Errors);
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }
}
