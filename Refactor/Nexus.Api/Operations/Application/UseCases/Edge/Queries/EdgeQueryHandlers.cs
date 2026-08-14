using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Operations.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;
using Refactor.Nexus.Api.Operations.Domain.Errors;
using Refactor.Nexus.Api.Operations.Domain.Services;

namespace Refactor.Nexus.Api.Operations.Application.UseCases.Edge.Queries;

public sealed record ResolveScriptQuery(string OperationKey);
public sealed record ResolveScriptResult(string ScriptId, string Name, string Body, string OperationKey);

public interface IResolveScriptUseCase
{
    Task<IOperationResult<ResolveScriptResult>> HandleAsync(ResolveScriptQuery query, CancellationToken cancellationToken = default);
}

public sealed class ResolveScriptHandler : IResolveScriptUseCase
{
    private readonly IOperationReadRepository _operations;
    private readonly IScriptArtifactRepository _scripts;
    private readonly IOperationActivityPolicy _activity;

    public ResolveScriptHandler(
        IOperationReadRepository operations,
        IScriptArtifactRepository scripts,
        IOperationActivityPolicy activity)
    {
        _operations = operations;
        _scripts = scripts;
        _activity = activity;
    }

    public async Task<IOperationResult<ResolveScriptResult>> HandleAsync(
        ResolveScriptQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!OperationKey.TryCreate(query.OperationKey, out var key))
        {
            return OperationResult<ResolveScriptResult>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.KeyEmpty)
                .WithMessage("Operation key obrigatoria.")
                .Build());
        }

        var operation = await _operations.GetByKeyAsync(key, cancellationToken);
        if (operation is null)
        {
            return OperationResult<ResolveScriptResult>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.NotFound)
                .WithMessage("Operacao nao encontrada para a key.")
                .Build());
        }

        if (!_activity.AllowsScriptResolve(operation.Status))
        {
            return OperationResult<ResolveScriptResult>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.ScriptResolveBlocked)
                .WithMessage("Script so resolve quando a Operacao esta Ativa.")
                .Build());
        }

        var script = await _scripts.GetEnabledByKeyAsync(key, cancellationToken);
        if (script is null)
        {
            return OperationResult<ResolveScriptResult>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.ScriptNotFound)
                .WithMessage("Nenhum script habilitado para esta key.")
                .Build());
        }

        return OperationResult<ResolveScriptResult>.Success(new ResolveScriptResult(
            script.Id.ToString(), script.Name, script.Body, script.OperationKey.Value));
    }
}

public sealed record GetStoreObjectQuery(string ObjectId);
public sealed record GetStoreObjectResult(string ObjectId, string OperationKey, string ObjectType, string PayloadJson);

public interface IGetStoreObjectUseCase
{
    Task<IOperationResult<GetStoreObjectResult>> HandleAsync(GetStoreObjectQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetStoreObjectHandler : IGetStoreObjectUseCase
{
    private readonly IStoreObjectRepository _store;

    public GetStoreObjectHandler(IStoreObjectRepository store)
    {
        _store = store;
    }

    public async Task<IOperationResult<GetStoreObjectResult>> HandleAsync(
        GetStoreObjectQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(query.ObjectId, out var objectId))
        {
            return OperationResult<GetStoreObjectResult>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.StoreObjectNotFound)
                .WithMessage("Objeto do Store nao encontrado.")
                .Build());
        }

        var item = await _store.GetByIdAsync(objectId, cancellationToken);
        if (item is null)
        {
            return OperationResult<GetStoreObjectResult>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.StoreObjectNotFound)
                .WithMessage("Objeto do Store nao encontrado.")
                .Build());
        }

        return OperationResult<GetStoreObjectResult>.Success(new GetStoreObjectResult(
            item.Id.ToString(), item.OperationKey.Value, item.ObjectType, item.PayloadJson));
    }
}
