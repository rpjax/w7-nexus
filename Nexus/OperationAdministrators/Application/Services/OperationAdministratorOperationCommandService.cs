using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.OperationAdministrators.Application.Contracts;
using Nexus.OperationAdministrators.Application.Requests;
using Nexus.OperationAdministrators.Application.Responses;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;

namespace Nexus.OperationAdministrators.Application.Services;

public sealed class OperationAdministratorOperationCommandService : IOperationAdministratorOperationCommandService
{
    private IOperationService _operationService { get; }

    public OperationAdministratorOperationCommandService(IOperationService operationService)
    {
        _operationService = operationService;
    }

    public async Task<IResult<SetOperationGatewaySelectionStrategyResponse>> SetOperationGatewaySelectionStrategyAsync(
        SetOperationGatewaySelectionStrategyRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<SetOperationGatewaySelectionStrategyResponse>();

        return ToResponse<SetOperationGatewaySelectionStrategyResponse>(
            await _operationService.SetGatewaySelectionStrategyAsync(request.OperationId, request.Strategy));
    }

    public async Task<IResult<AssignStrawManToOperationResponse>> AssignStrawManToOperationAsync(
        AssignStrawManToOperationRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignStrawManToOperationResponse>();

        return ToResponse<AssignStrawManToOperationResponse>(
            await _operationService.AssignStrawManAsync(request.OperationId, request.StrawManId));
    }

    public async Task<IResult<UnassignStrawManFromOperationResponse>> UnassignStrawManFromOperationAsync(
        UnassignStrawManFromOperationRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignStrawManFromOperationResponse>();

        return ToResponse<UnassignStrawManFromOperationResponse>(
            await _operationService.UnassignStrawManAsync(request.OperationId, request.StrawManId));
    }

    public async Task<IResult<AssignGatewayAccountGroupToOperationResponse>> AssignGatewayAccountGroupToOperationAsync(
        AssignGatewayAccountGroupToOperationRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignGatewayAccountGroupToOperationResponse>();

        return ToResponse<AssignGatewayAccountGroupToOperationResponse>(
            await _operationService.AssignGatewayCredentialsGroupAsync(
                request.OperationId,
                request.GatewayCredentialsGroupId));
    }

    public async Task<IResult<UnassignGatewayAccountGroupFromOperationResponse>> UnassignGatewayAccountGroupFromOperationAsync(
        UnassignGatewayAccountGroupFromOperationRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignGatewayAccountGroupFromOperationResponse>();

        return ToResponse<UnassignGatewayAccountGroupFromOperationResponse>(
            await _operationService.UnassignGatewayCredentialsGroupAsync(
                request.OperationId,
                request.GatewayCredentialsGroupId));
    }

    public async Task<IResult<AssignGatewayAccountToOperationResponse>> AssignGatewayAccountToOperationAsync(
        AssignGatewayAccountToOperationRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignGatewayAccountToOperationResponse>();

        return ToResponse<AssignGatewayAccountToOperationResponse>(
            await _operationService.AssignGatewayCredentialsAsync(
                request.OperationId,
                request.GatewayCredentialsId));
    }

    public async Task<IResult<UnassignGatewayAccountFromOperationResponse>> UnassignGatewayAccountFromOperationAsync(
        UnassignGatewayAccountFromOperationRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignGatewayAccountFromOperationResponse>();

        return ToResponse<UnassignGatewayAccountFromOperationResponse>(
            await _operationService.UnassignGatewayCredentialsAsync(
                request.OperationId,
                request.GatewayCredentialsId));
    }

    private static IResult<TResponse> ToResponse<TResponse>(IResult result)
        where TResponse : new()
    {
        if (result.IsFailure)
            return Result<TResponse>.Failure(result.Errors);

        return Result<TResponse>.Success(new TResponse());
    }

    private static IResult<T> RequestBodyRequiredResult<T>()
    {
        return Result<T>.Failure(Error.Create()
            .WithCode(OperationErrorCodes.RequestBodyRequired)
            .WithMessage("O corpo da requisição é obrigatório.")
            .Build());
    }
}
