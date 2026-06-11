using Aidan.Core.Linq.Extensions;
using Aidan.Core.Errors;
using Microsoft.AspNetCore.Mvc;

using Aidan.Web.Controllers;
using Nexus.Actors.Requests;
using Nexus.Operations.Application;
using Nexus.Operations.ErrorCodes;
using Nexus.Operations.Application.Models;

namespace Nexus.Operations.Presentation;

[Route("api/operations")]
public class OperationController : WebController
{
    private IOperationService _operationService { get; }
    private IOperationRepository _operationRepository { get; }

    public OperationController(
        IOperationService operationService,
        IOperationRepository operationRepository)
    {
        _operationService = operationService;
        _operationRepository = operationRepository;
    }

    private static Error RequestBodyRequiredError() =>
        Error.Create()
            .WithCode(OperationErrorCodes.RequestBodyRequired)
            .WithMessage("Request body is required.")
            .Build();

    [HttpPost("search")]
    public async Task<ActionResult> GetOperations([FromBody] SearchOperationsRequest? request)
    {
        request ??= new SearchOperationsRequest();

        var limit = request.Limit <= 0 ? 20 : request.Limit;
        var offset = request.Offset;
        var keyword = request.Keyword?.Trim();

        if (limit < 0 || limit >= 1000)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("Operation.SEARCH_LIMIT_INVALID")
                .WithMessage("Limit must be between 1 and 999.")
                .Build());
        }

        if (offset < 0)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("Operation.SEARCH_OFFSET_INVALID")
                .WithMessage("Offset cannot be negative.")
                .Build());
        }

        if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length > 200)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("Operation.SEARCH_KEYWORD_TOO_LONG")
                .WithMessage("Keyword can have at most 200 characters.")
                .Build());
        }

        var query = _operationRepository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.ToLowerInvariant();
            query = query.Where(o =>
                o.Id.ToLower().Contains(term) ||
                o.Name.ToLower().Contains(term) ||
                (o.Description != null && o.Description.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.UpdatedAt)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync();

        return Ok(new
        {
            Total = total,
            Items = items,
        });
    }

    [HttpPost]
    public async Task<ActionResult> CreateOperation([FromBody] CreateOperationRequest? request)
    {
        if (request is null)
        {
            return ProblemResponse(422, RequestBodyRequiredError());
        }

        var result = await _operationService.CreateOperationAsync(
            name: request.Name,
            description: request.Description,
            operatorIds: request.Operators);

        if (result.IsFailure)
        {
            return ProblemResponse(422, result.Errors);
        }

        return Created();
    }

    [HttpPost("operators")]
    public async Task<ActionResult> AssignOperatorAsync(
        [FromBody] AssignOperatorRequest? request)
    {
        if (request is null)
        {
            return ProblemResponse(422, RequestBodyRequiredError());
        }

        var operationId = request.OperationId;
        var operatorId = request.OperatorId;
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.OperationIdInvalid)
                .WithMessage("Operation ID is required.")
                .Build());
        }

        if (string.IsNullOrWhiteSpace(operatorId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.OperatorInvalid)
                .WithMessage("Operator ID is required.")
                .Build());
        }

        var result = await _operationService.AssignOperatorAsync(operationId, operatorId);

        if (result.IsFailure)
        {
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }

    [HttpDelete("operators")]
    public async Task<ActionResult> UnassignOperatorAsync(
        [FromBody] UnassignOperatorRequest? request)
    {
        if (request is null)
        {
            return ProblemResponse(422, RequestBodyRequiredError());
        }

        var operationId = request.OperationId;
        var operatorId = request.OperatorId;
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.OperationIdInvalid)
                .WithMessage("Operation ID is required.")
                .Build());
        }

        if (string.IsNullOrWhiteSpace(operatorId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.OperatorInvalid)
                .WithMessage("Operator ID is required.")
                .Build());
        }

        var result = await _operationService.UnassignOperatorAsync(operationId, operatorId);

        if (result.IsFailure)
        {
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }

    [HttpPost("strawman")]
    public async Task<ActionResult> AssignStrawManAsync(
        [FromBody] AssignStrawManRequest? request)
    {
        if (request is null)
        {
            return ProblemResponse(422, RequestBodyRequiredError());
        }

        var operationId = request.OperationId;
        var strawManId = request.StrawManId;
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.OperationIdInvalid)
                .WithMessage("Operation ID is required.")
                .Build());
        }

        if (string.IsNullOrWhiteSpace(strawManId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.StrawManInvalid)
                .WithMessage("Straw man ID is required.")
                .Build());
        }

        var result = await _operationService.AssignStrawManAsync(operationId, strawManId);

        if (result.IsFailure)
        {
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }

    [HttpDelete("strawman")]
    public async Task<ActionResult> UnassignStrawManAsync(
        [FromBody] UnassignStrawManRequest? request)
    {
        if (request is null)
        {
            return ProblemResponse(422, RequestBodyRequiredError());
        }

        var operationId = request.OperationId;
        var strawManId = request.StrawManId;
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.OperationIdInvalid)
                .WithMessage("Operation ID is required.")
                .Build());
        }

        if (string.IsNullOrWhiteSpace(strawManId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.StrawManInvalid)
                .WithMessage("Straw man ID is required.")
                .Build());
        }

        var result = await _operationService.UnassignStrawManAsync(operationId, strawManId);

        if (result.IsFailure)
        {
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }

    [HttpPost("gateway-selection-strategy")]
    public async Task<ActionResult> SetGatewaySelectionStrategyAsync(
        [FromBody] SetGatewaySelectionStrategyRequest? request)
    {
        if (request is null)
        {
            return ProblemResponse(422, RequestBodyRequiredError());
        }

        var operationId = request.OperationId;
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.OperationIdInvalid)
                .WithMessage("Operation ID is required.")
                .Build());
        }

        var result = await _operationService.SetGatewaySelectionStrategyAsync(operationId, request.Strategy);

        if (result.IsFailure)
        {
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }

    [HttpPost("gateway-credentials-groups")]
    public async Task<ActionResult> AssignGatewayCredentialsGroupAsync(
        [FromBody] AssignGatewayCredentialsGroupRequest? request)
    {
        if (request is null)
        {
            return ProblemResponse(422, RequestBodyRequiredError());
        }

        var operationId = request.OperationId;
        var groupId = request.GatewayCredentialsGroupId;
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.OperationIdInvalid)
                .WithMessage("Operation ID is required.")
                .Build());
        }

        if (string.IsNullOrWhiteSpace(groupId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupInvalid)
                .WithMessage("Gateway credentials group ID is required.")
                .Build());
        }

        var result = await _operationService.AssignGatewayCredentialsGroupAsync(operationId, groupId);

        if (result.IsFailure)
        {
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }

    [HttpDelete("gateway-credentials-groups")]
    public async Task<ActionResult> UnassignGatewayCredentialsGroupAsync(
        [FromBody] UnassignGatewayCredentialsGroupRequest? request)
    {
        if (request is null)
        {
            return ProblemResponse(422, RequestBodyRequiredError());
        }

        var operationId = request.OperationId;
        var groupId = request.GatewayCredentialsGroupId;
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.OperationIdInvalid)
                .WithMessage("Operation ID is required.")
                .Build());
        }

        if (string.IsNullOrWhiteSpace(groupId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.GatewayCredentialsGroupInvalid)
                .WithMessage("Gateway credentials group ID is required.")
                .Build());
        }

        var result = await _operationService.UnassignGatewayCredentialsGroupAsync(operationId, groupId);

        if (result.IsFailure)
        {
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }

    [HttpPost("gateway-credentials")]
    public async Task<ActionResult> AssignGatewayCredentialsAsync(
        [FromBody] AssignGatewayCredentialsRequest? request)
    {
        if (request is null)
        {
            return ProblemResponse(422, RequestBodyRequiredError());
        }

        var operationId = request.OperationId;
        var credentialsId = request.GatewayCredentialsId;
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.OperationIdInvalid)
                .WithMessage("Operation ID is required.")
                .Build());
        }

        if (string.IsNullOrWhiteSpace(credentialsId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.ChargeCredentialInvalid)
                .WithMessage("Gateway credentials ID is required.")
                .Build());
        }

        var result = await _operationService.AssignGatewayCredentialsAsync(operationId, credentialsId);

        if (result.IsFailure)
        {
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }

    [HttpDelete("gateway-credentials")]
    public async Task<ActionResult> UnassignGatewayCredentialsAsync(
        [FromBody] UnassignGatewayCredentialsRequest? request)
    {
        if (request is null)
        {
            return ProblemResponse(422, RequestBodyRequiredError());
        }

        var operationId = request.OperationId;
        var credentialsId = request.GatewayCredentialsId;
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.OperationIdInvalid)
                .WithMessage("Operation ID is required.")
                .Build());
        }

        if (string.IsNullOrWhiteSpace(credentialsId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.ChargeCredentialInvalid)
                .WithMessage("Gateway credentials ID is required.")
                .Build());
        }

        var result = await _operationService.UnassignGatewayCredentialsAsync(operationId, credentialsId);

        if (result.IsFailure)
        {
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }

    [HttpDelete("operations")]
    public async Task<ActionResult> DeleteOperationAsync(
        [FromBody] DeleteOperationRequest? request)
    {
        if (request is null)
        {
            return ProblemResponse(422, RequestBodyRequiredError());
        }

        var operationId = request.OperationId;
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return ProblemResponse(422, Error.Create()
                .WithCode(OperationErrorCodes.OperationIdInvalid)
                .WithMessage("Operation ID is required.")
                .Build());
        }

        var result = await _operationService.DeleteOperationAsync(operationId);

        if (result.IsFailure)
        {
            return ProblemResponse(422, result.Errors);
        }

        return NoContent();
    }
}
