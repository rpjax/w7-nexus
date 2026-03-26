using Aidan.Core.Patterns;
using Aidan.Core.Linq.Extensions;
using Microsoft.AspNetCore.Mvc;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application;
using Nexus.Operations.Application.Models;
using Nexus.Operations.ErrorCodes;
using System.Linq.Expressions;

namespace Nexus.Operations.Presentation;

[ApiController]
[Route("api/operations")]
public sealed class OperationController : ControllerBase
{
    private static readonly Expression<Func<Operation, OperationResponse>> ToResponseProjection = o =>
        new OperationResponse
        {
            Id = o.Id,
            Name = o.Name,
            Description = o.Description,
            Operators = o.Operators.ToArray(),
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt
        };

    private readonly IOperationService _operationService;
    private readonly IOperationRepository _operationRepository;

    public OperationController(
        IOperationService operationService,
        IOperationRepository operationRepository)
    {
        _operationService = operationService;
        _operationRepository = operationRepository;
    }

    [HttpGet]
    public async Task<ActionResult> ListAsync(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page <= 0)
            return BadRequest("page must be greater than 0.");
        if (pageSize <= 0 || pageSize > 100)
            return BadRequest("pageSize must be between 1 and 100.");

        var query = _operationRepository.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(o =>
                o.Id.ToLower().Contains(term) ||
                o.Name.ToLower().Contains(term) ||
                o.Description.ToLower().Contains(term) ||
                o.Operators.Any(op => op.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();
        var skip = (page - 1) * pageSize;

        var items = await query
            .OrderByDescending(o => o.UpdatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(ToResponseProjection)
            .ToArrayAsync();

        return Ok(new PagedOperationResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateOperationHttpRequest request)
    {
        var result = await _operationService.CreateOperationAsync(new CreateOperationRequest
        {
            Name = request.Name,
            Description = request.Description,
            Operators = request.Operators
        });

        if (result.IsFailure)
            return ToFailureResponse(result);

        var operation = result.Value!;
        return CreatedAtAction(
            nameof(CreateAsync),
            new { operationId = operation.Id },
            ToResponse(operation));
    }

    [HttpPost("operators/{operatorId}")]
    public async Task<IActionResult> AddOperatorAsync([FromQuery] string operationId, string operatorId)
    {
        var result = await _operationService.AddOperatorAsync(operationId, operatorId);
        if (result.IsFailure)
            return ToFailureResponse(result);

        return NoContent();
    }

    [HttpDelete("operators/{operatorId}")]
    public async Task<IActionResult> RemoveOperatorAsync([FromQuery] string operationId, string operatorId)
    {
        var result = await _operationService.RemoveOperatorAsync(operationId, operatorId);
        if (result.IsFailure)
            return ToFailureResponse(result);

        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync([FromQuery] string operationId)
    {
        var result = await _operationService.DeleteOperationAsync(operationId);
        if (result.IsFailure)
            return ToFailureResponse(result);

        return NoContent();
    }

    private IActionResult ToFailureResponse(IResult result)
    {
        var errors = result.Errors.ToArray();
        if (errors.Length == 0)
            return BadRequest("Operation request failed.");

        var first = errors[0];
        if (first.Code == OperationErrorCodes.OperationNotFound)
            return NotFound(errors);

        return BadRequest(errors);
    }

    private static OperationResponse ToResponse(Operation operation)
    {
        return new OperationResponse
        {
            Id = operation.Id,
            Name = operation.Name,
            Description = operation.Description,
            Operators = operation.Operators.ToArray(),
            CreatedAt = operation.CreatedAt,
            UpdatedAt = operation.UpdatedAt
        };
    }
}

public sealed class CreateOperationHttpRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public IEnumerable<string>? Operators { get; set; }
}

public sealed class OperationResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Operators { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class PagedOperationResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public OperationResponse[] Items { get; set; } = Array.Empty<OperationResponse>();
}
