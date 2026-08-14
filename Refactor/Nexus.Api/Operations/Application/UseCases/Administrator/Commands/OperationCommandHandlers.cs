using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Operations.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Operations.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Operations.Application.UseCases.Shared;
using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;
using Refactor.Nexus.Api.Operations.Domain.Errors;
using Refactor.Nexus.Api.Operations.Domain.Services;
using OperationAggregate = Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation.Operation;
using ScriptArtifact = Refactor.Nexus.Api.Operations.Domain.Aggregates.Script.ScriptArtifact;
using StoreObject = Refactor.Nexus.Api.Operations.Domain.Aggregates.Store.StoreObject;

namespace Refactor.Nexus.Api.Operations.Application.UseCases.Administrator.Commands;

public sealed record CreateOperationCommand(string Name, decimal? ManagementCutPercent);
public sealed record CreateOperationResult(string OperationId, string OperationKey, string Status);

public interface ICreateOperationUseCase
{
    Task<IOperationResult<CreateOperationResult>> HandleAsync(CreateOperationCommand command, CancellationToken cancellationToken = default);
}

public sealed class CreateOperationHandler : ICreateOperationUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateCapabilityGate _gate;
    private readonly IOperationRepository _repository;

    public CreateOperationHandler(
        IRequestContext requestContext,
        IMandateCapabilityGate gate,
        IOperationRepository repository )
    {
        _requestContext = requestContext;
        _gate = gate;
        _repository = repository;
    }

    public async Task<IOperationResult<CreateOperationResult>> HandleAsync(
        CreateOperationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<CreateOperationResult>.Failure(OperationAccessGuards.RequestBodyRequired());

        var (requester, failure) = await OperationAccessGuards.AuthorizeManageAsync<CreateOperationResult>(
            _requestContext, _gate, operationId: null, cancellationToken);
        if (failure is not null)
            return failure;
        _ = requester;

        var created = OperationAggregate.Create(command.Name, command.ManagementCutPercent);
        if (created.IsFailure)
            return OperationResult<CreateOperationResult>.Failure(created.Errors);

        await _repository.SaveAsync(created.Value!, cancellationToken);
        return OperationResult<CreateOperationResult>.Success(new CreateOperationResult(
            created.Value!.Id.ToString(),
            created.Value.Key.Value,
            created.Value.Status.ToString()));
    }
}

public sealed record TransitionOperationCommand(string OperationId, string TargetStatus);
public sealed record TransitionOperationResult(string OperationId, string Status);

public interface ITransitionOperationUseCase
{
    Task<IOperationResult<TransitionOperationResult>> HandleAsync(TransitionOperationCommand command, CancellationToken cancellationToken = default);
}

public sealed class TransitionOperationHandler : ITransitionOperationUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateCapabilityGate _gate;
    private readonly IOperationRepository _repository;

    public TransitionOperationHandler(
        IRequestContext requestContext,
        IMandateCapabilityGate gate,
        IOperationRepository repository )
    {
        _requestContext = requestContext;
        _gate = gate;
        _repository = repository;
    }

    public async Task<IOperationResult<TransitionOperationResult>> HandleAsync(
        TransitionOperationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<TransitionOperationResult>.Failure(OperationAccessGuards.RequestBodyRequired());

        if (!OperationId.TryParse(command.OperationId, out var operationId))
            return OperationResult<TransitionOperationResult>.Failure(OperationAccessGuards.NotFound(command.OperationId));

        var access = await OperationAccessGuards.AuthorizeManageAsync<TransitionOperationResult>(
            _requestContext, _gate, operationId, cancellationToken);
        if (access.Failure is not null)
            return access.Failure;

        if (!Enum.TryParse<OperationStatus>(command.TargetStatus, ignoreCase: true, out var target))
        {
            return OperationResult<TransitionOperationResult>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.InvalidTransition)
                .WithMessage($"Status '{command.TargetStatus}' invalido.")
                .Build());
        }

        var operation = await _repository.GetByIdAsync(operationId, cancellationToken);
        if (operation is null)
            return OperationResult<TransitionOperationResult>.Failure(OperationAccessGuards.NotFound(command.OperationId));

        var from = operation.Status.ToString();
        var mutation = operation.TransitionTo(target);
        if (mutation.IsFailure)
            return OperationResult<TransitionOperationResult>.Failure(mutation.Errors);

        await _repository.SaveAsync(operation, cancellationToken);
        return OperationResult<TransitionOperationResult>.Success(
            new TransitionOperationResult(operation.Id.ToString(), operation.Status.ToString()));
    }
}

public sealed record ConfigureManagementCutCommand(string OperationId, decimal? ManagementCutPercent);
public sealed class ConfigureManagementCutResult;

public interface IConfigureManagementCutUseCase
{
    Task<IOperationResult<ConfigureManagementCutResult>> HandleAsync(ConfigureManagementCutCommand command, CancellationToken cancellationToken = default);
}

public sealed class ConfigureManagementCutHandler : IConfigureManagementCutUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateCapabilityGate _gate;
    private readonly IOperationRepository _repository;

    public ConfigureManagementCutHandler(
        IRequestContext requestContext,
        IMandateCapabilityGate gate,
        IOperationRepository repository)
    {
        _requestContext = requestContext;
        _gate = gate;
        _repository = repository;
    }

    public async Task<IOperationResult<ConfigureManagementCutResult>> HandleAsync(
        ConfigureManagementCutCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<ConfigureManagementCutResult>.Failure(OperationAccessGuards.RequestBodyRequired());

        if (!OperationId.TryParse(command.OperationId, out var operationId))
            return OperationResult<ConfigureManagementCutResult>.Failure(OperationAccessGuards.NotFound(command.OperationId));

        var access = await OperationAccessGuards.AuthorizeManageAsync<ConfigureManagementCutResult>(
            _requestContext, _gate, operationId, cancellationToken);
        if (access.Failure is not null)
            return access.Failure;

        var operation = await _repository.GetByIdAsync(operationId, cancellationToken);
        if (operation is null)
            return OperationResult<ConfigureManagementCutResult>.Failure(OperationAccessGuards.NotFound(command.OperationId));

        var mutation = operation.ConfigureManagementCut(command.ManagementCutPercent);
        if (mutation.IsFailure)
            return OperationResult<ConfigureManagementCutResult>.Failure(mutation.Errors);

        await _repository.SaveAsync(operation, cancellationToken);
        return OperationResult<ConfigureManagementCutResult>.Success(new ConfigureManagementCutResult());
    }
}

public sealed record AssignOperatorCommand(string OperationId, string MemberId);
public sealed class AssignOperatorResult;
public interface IAssignOperatorUseCase
{
    Task<IOperationResult<AssignOperatorResult>> HandleAsync(AssignOperatorCommand command, CancellationToken cancellationToken = default);
}

public sealed class AssignOperatorHandler : IAssignOperatorUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateCapabilityGate _gate;
    private readonly IOperatorEligibility _eligibility;
    private readonly IOperationRepository _repository;

    public AssignOperatorHandler(
        IRequestContext requestContext,
        IMandateCapabilityGate gate,
        IOperatorEligibility eligibility,
        IOperationRepository repository )
    {
        _requestContext = requestContext;
        _gate = gate;
        _eligibility = eligibility;
        _repository = repository;
    }

    public async Task<IOperationResult<AssignOperatorResult>> HandleAsync(
        AssignOperatorCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<AssignOperatorResult>.Failure(OperationAccessGuards.RequestBodyRequired());

        if (!OperationId.TryParse(command.OperationId, out var operationId))
            return OperationResult<AssignOperatorResult>.Failure(OperationAccessGuards.NotFound(command.OperationId));
        if (!Guid.TryParse(command.MemberId, out var memberId))
        {
            return OperationResult<AssignOperatorResult>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.OperatorNotEligible)
                .WithMessage("MemberId invalido.")
                .Build());
        }

        var access = await OperationAccessGuards.AuthorizeManageAsync<AssignOperatorResult>(
            _requestContext, _gate, operationId, cancellationToken);
        if (access.Failure is not null)
            return access.Failure;

        if (!await _eligibility.IsEligibleOperatorAsync(memberId, cancellationToken))
        {
            return OperationResult<AssignOperatorResult>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.OperatorNotEligible)
                .WithMessage("Membro precisa ser Operator com AgencyDeal ativo.")
                .Build());
        }

        if (await _gate.IsAdministratorAsync(memberId, cancellationToken)
            || await _gate.HasManagementOverOperationAsync(memberId, operationId, cancellationToken))
        {
            return OperationResult<AssignOperatorResult>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.TipManagementConflict)
                .WithMessage("Conflito ponta x gestao: membro nao pode gerir e atuar como ponta na mesma op.")
                .Build());
        }

        var operation = await _repository.GetByIdAsync(operationId, cancellationToken);
        if (operation is null)
            return OperationResult<AssignOperatorResult>.Failure(OperationAccessGuards.NotFound(command.OperationId));

        var mutation = operation.AssignOperator(memberId);
        if (mutation.IsFailure)
            return OperationResult<AssignOperatorResult>.Failure(mutation.Errors);

        await _repository.SaveAsync(operation, cancellationToken);
        return OperationResult<AssignOperatorResult>.Success(new AssignOperatorResult());
    }
}

public sealed record UnassignOperatorCommand(string OperationId, string MemberId);
public sealed class UnassignOperatorResult;
public interface IUnassignOperatorUseCase
{
    Task<IOperationResult<UnassignOperatorResult>> HandleAsync(UnassignOperatorCommand command, CancellationToken cancellationToken = default);
}

public sealed class UnassignOperatorHandler : IUnassignOperatorUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateCapabilityGate _gate;
    private readonly IOperationRepository _repository;

    public UnassignOperatorHandler(
        IRequestContext requestContext,
        IMandateCapabilityGate gate,
        IOperationRepository repository )
    {
        _requestContext = requestContext;
        _gate = gate;
        _repository = repository;
    }

    public async Task<IOperationResult<UnassignOperatorResult>> HandleAsync(
        UnassignOperatorCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<UnassignOperatorResult>.Failure(OperationAccessGuards.RequestBodyRequired());

        if (!OperationId.TryParse(command.OperationId, out var operationId))
            return OperationResult<UnassignOperatorResult>.Failure(OperationAccessGuards.NotFound(command.OperationId));
        if (!Guid.TryParse(command.MemberId, out var memberId))
        {
            return OperationResult<UnassignOperatorResult>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.NotAssigned)
                .WithMessage("MemberId invalido.")
                .Build());
        }

        var access = await OperationAccessGuards.AuthorizeManageAsync<UnassignOperatorResult>(
            _requestContext, _gate, operationId, cancellationToken);
        if (access.Failure is not null)
            return access.Failure;

        var operation = await _repository.GetByIdAsync(operationId, cancellationToken);
        if (operation is null)
            return OperationResult<UnassignOperatorResult>.Failure(OperationAccessGuards.NotFound(command.OperationId));

        var mutation = operation.UnassignOperator(memberId);
        if (mutation.IsFailure)
            return OperationResult<UnassignOperatorResult>.Failure(mutation.Errors);

        await _repository.SaveAsync(operation, cancellationToken);
        return OperationResult<UnassignOperatorResult>.Success(new UnassignOperatorResult());
    }
}

public sealed record RegisterScriptCommand(string OperationId, string Name, string Body);
public sealed record RegisterScriptResult(string ScriptId);

public interface IRegisterScriptUseCase
{
    Task<IOperationResult<RegisterScriptResult>> HandleAsync(RegisterScriptCommand command, CancellationToken cancellationToken = default);
}

public sealed class RegisterScriptHandler : IRegisterScriptUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateCapabilityGate _gate;
    private readonly IOperationReadRepository _operations;
    private readonly IScriptArtifactRepository _scripts;

    public RegisterScriptHandler(
        IRequestContext requestContext,
        IMandateCapabilityGate gate,
        IOperationReadRepository operations,
        IScriptArtifactRepository scripts )
    {
        _requestContext = requestContext;
        _gate = gate;
        _operations = operations;
        _scripts = scripts;
    }

    public async Task<IOperationResult<RegisterScriptResult>> HandleAsync(
        RegisterScriptCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<RegisterScriptResult>.Failure(OperationAccessGuards.RequestBodyRequired());

        if (!OperationId.TryParse(command.OperationId, out var operationId))
            return OperationResult<RegisterScriptResult>.Failure(OperationAccessGuards.NotFound(command.OperationId));

        var access = await OperationAccessGuards.AuthorizeManageAsync<RegisterScriptResult>(
            _requestContext, _gate, operationId, cancellationToken);
        if (access.Failure is not null)
            return access.Failure;

        var operation = await _operations.GetByIdAsync(operationId, cancellationToken);
        if (operation is null)
            return OperationResult<RegisterScriptResult>.Failure(OperationAccessGuards.NotFound(command.OperationId));

        var created = ScriptArtifact.Create(operation.Key, command.Name, command.Body);
        if (created.IsFailure)
            return OperationResult<RegisterScriptResult>.Failure(created.Errors);

        await _scripts.SaveAsync(created.Value!, cancellationToken);
        return OperationResult<RegisterScriptResult>.Success(new RegisterScriptResult(created.Value!.Id.ToString()));
    }
}

public sealed record UpsertStoreObjectCommand(string OperationId, string? ObjectId, string ObjectType, string PayloadJson);
public sealed record UpsertStoreObjectResult(string ObjectId);

public interface IUpsertStoreObjectUseCase
{
    Task<IOperationResult<UpsertStoreObjectResult>> HandleAsync(UpsertStoreObjectCommand command, CancellationToken cancellationToken = default);
}

public sealed class UpsertStoreObjectHandler : IUpsertStoreObjectUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateCapabilityGate _gate;
    private readonly IOperationReadRepository _operations;
    private readonly IStoreObjectRepository _store;
    private readonly IOperationActivityPolicy _activity;

    public UpsertStoreObjectHandler(
        IRequestContext requestContext,
        IMandateCapabilityGate gate,
        IOperationReadRepository operations,
        IStoreObjectRepository store,
        IOperationActivityPolicy activity )
    {
        _requestContext = requestContext;
        _gate = gate;
        _operations = operations;
        _store = store;
        _activity = activity;
    }

    public async Task<IOperationResult<UpsertStoreObjectResult>> HandleAsync(
        UpsertStoreObjectCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<UpsertStoreObjectResult>.Failure(OperationAccessGuards.RequestBodyRequired());

        if (!OperationId.TryParse(command.OperationId, out var operationId))
            return OperationResult<UpsertStoreObjectResult>.Failure(OperationAccessGuards.NotFound(command.OperationId));

        var access = await OperationAccessGuards.AuthorizeManageAsync<UpsertStoreObjectResult>(
            _requestContext, _gate, operationId, cancellationToken);
        if (access.Failure is not null)
            return access.Failure;

        var operation = await _operations.GetByIdAsync(operationId, cancellationToken);
        if (operation is null)
            return OperationResult<UpsertStoreObjectResult>.Failure(OperationAccessGuards.NotFound(command.OperationId));

        if (!_activity.AllowsStoreWrite(operation.Status))
        {
            return OperationResult<UpsertStoreObjectResult>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.StoreWriteBlocked)
                .WithMessage("Store e read-only quando a Operacao esta Pausada ou Encerrada.")
                .Build());
        }

        StoreObject storeObject;
        if (!string.IsNullOrWhiteSpace(command.ObjectId) && Guid.TryParse(command.ObjectId, out var existingId))
        {
            var existing = await _store.GetByIdAsync(existingId, cancellationToken);
            if (existing is null)
            {
                return OperationResult<UpsertStoreObjectResult>.Failure(Error.Create()
                    .WithCode(OperationErrorCodes.StoreObjectNotFound)
                    .WithMessage("Objeto do Store nao encontrado.")
                    .Build());
            }

            if (!string.Equals(existing.OperationKey.Value, operation.Key.Value, StringComparison.Ordinal))
            {
                return OperationResult<UpsertStoreObjectResult>.Failure(Error.Create()
                    .WithCode(OperationErrorCodes.StoreKeyMismatch)
                    .WithMessage("Objeto pertence a outra operation key.")
                    .Build());
            }

            existing.Update(command.ObjectType, command.PayloadJson);
            storeObject = existing;
        }
        else
        {
            var created = StoreObject.Create(operation.Key, command.ObjectType, command.PayloadJson);
            if (created.IsFailure)
                return OperationResult<UpsertStoreObjectResult>.Failure(created.Errors);
            storeObject = created.Value!;
        }

        await _store.SaveAsync(storeObject, cancellationToken);
        return OperationResult<UpsertStoreObjectResult>.Success(new UpsertStoreObjectResult(storeObject.Id.ToString()));
    }
}

public sealed record DeleteStoreObjectCommand(string OperationId, string ObjectId);
public sealed class DeleteStoreObjectResult;
public interface IDeleteStoreObjectUseCase
{
    Task<IOperationResult<DeleteStoreObjectResult>> HandleAsync(DeleteStoreObjectCommand command, CancellationToken cancellationToken = default);
}

public sealed class DeleteStoreObjectHandler : IDeleteStoreObjectUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateCapabilityGate _gate;
    private readonly IOperationReadRepository _operations;
    private readonly IStoreObjectRepository _store;
    private readonly IOperationActivityPolicy _activity;

    public DeleteStoreObjectHandler(
        IRequestContext requestContext,
        IMandateCapabilityGate gate,
        IOperationReadRepository operations,
        IStoreObjectRepository store,
        IOperationActivityPolicy activity )
    {
        _requestContext = requestContext;
        _gate = gate;
        _operations = operations;
        _store = store;
        _activity = activity;
    }

    public async Task<IOperationResult<DeleteStoreObjectResult>> HandleAsync(
        DeleteStoreObjectCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!OperationId.TryParse(command.OperationId, out var operationId))
            return OperationResult<DeleteStoreObjectResult>.Failure(OperationAccessGuards.NotFound(command.OperationId));
        if (!Guid.TryParse(command.ObjectId, out var objectId))
        {
            return OperationResult<DeleteStoreObjectResult>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.StoreObjectNotFound)
                .WithMessage("Objeto do Store nao encontrado.")
                .Build());
        }

        var access = await OperationAccessGuards.AuthorizeManageAsync<DeleteStoreObjectResult>(
            _requestContext, _gate, operationId, cancellationToken);
        if (access.Failure is not null)
            return access.Failure;

        var operation = await _operations.GetByIdAsync(operationId, cancellationToken);
        if (operation is null)
            return OperationResult<DeleteStoreObjectResult>.Failure(OperationAccessGuards.NotFound(command.OperationId));

        if (!_activity.AllowsStoreWrite(operation.Status))
        {
            return OperationResult<DeleteStoreObjectResult>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.StoreWriteBlocked)
                .WithMessage("Store e read-only quando a Operacao esta Pausada ou Encerrada.")
                .Build());
        }

        var existing = await _store.GetByIdAsync(objectId, cancellationToken);
        if (existing is null || !string.Equals(existing.OperationKey.Value, operation.Key.Value, StringComparison.Ordinal))
        {
            return OperationResult<DeleteStoreObjectResult>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.StoreObjectNotFound)
                .WithMessage("Objeto do Store nao encontrado nesta Operacao.")
                .Build());
        }

        await _store.DeleteAsync(objectId, cancellationToken);
        return OperationResult<DeleteStoreObjectResult>.Success(new DeleteStoreObjectResult());
    }
}
