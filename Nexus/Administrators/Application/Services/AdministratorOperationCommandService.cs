using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Administrators.Application.Contracts;
using Nexus.Administrators.Application.Mapping;
using Nexus.Administrators.Application.Requests;
using Nexus.Administrators.Application.Responses;
using Nexus.Administrators.Application.Responses.Models;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Errors;

namespace Nexus.Administrators.Application.Services;

public sealed class AdministratorOperationCommandService : IAdministratorOperationCommandService
{
    private IOperationService _operationService { get; }

    public AdministratorOperationCommandService(IOperationService operationService)
    {
        _operationService = operationService;
    }

    public async Task<IResult<OperationDetails>> CreateOperationAsync(
        CreateOperationRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<OperationDetails>();

        var result = await _operationService.CreateOperationAsync(
            name: request.Name,
            description: request.Description);

        if (result.IsFailure)
            return Result<OperationDetails>.Failure(result.Errors);

        return Result<OperationDetails>.Success(result.Value!.ToOperationDetails());
    }

    public async Task<IResult<DeleteOperationResponse>> DeleteOperationAsync(
        DeleteOperationRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<DeleteOperationResponse>();

        var result = await _operationService.DeleteOperationAsync(request.OperationId);
        if (result.IsFailure)
            return Result<DeleteOperationResponse>.Failure(result.Errors);

        return Result<DeleteOperationResponse>.Success(new DeleteOperationResponse());
    }

    public async Task<IResult<AssignOperationAdministratorResponse>> AssignOperationAdministratorAsync(
        AssignOperationAdministratorRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<AssignOperationAdministratorResponse>();

        var result = await _operationService.AssignAdministratorAsync(
            request.OperationId,
            request.AdministratorId);

        if (result.IsFailure)
            return Result<AssignOperationAdministratorResponse>.Failure(result.Errors);

        return Result<AssignOperationAdministratorResponse>.Success(new AssignOperationAdministratorResponse());
    }

    public async Task<IResult<UnassignOperationAdministratorResponse>> UnassignOperationAdministratorAsync(
        UnassignOperationAdministratorRequest request)
    {
        if (request is null)
            return RequestBodyRequiredResult<UnassignOperationAdministratorResponse>();

        var result = await _operationService.UnassignAdministratorAsync(
            request.OperationId,
            request.AdministratorId);

        if (result.IsFailure)
            return Result<UnassignOperationAdministratorResponse>.Failure(result.Errors);

        return Result<UnassignOperationAdministratorResponse>.Success(new UnassignOperationAdministratorResponse());
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
