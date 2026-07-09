using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Scripts.Aggregates;
using Nexus.Scripts.Application.Contracts;
using Nexus.Scripts.Application.Requests;
using Nexus.Scripts.Application.Responses;
using Nexus.Scripts.Errors;

namespace Nexus.Scripts.Application.Services;

public sealed class ScriptAdministrator : IScriptAdministrator
{
    public const int MaxSearchKeywordLength = 200;
    public const int MaxSearchLimit = 100;

    private readonly IAdministratorAccessPolicy _policy;
    private readonly IScriptRepository _scripts;
    private readonly IReleaseRepository _releases;
    private readonly ScriptCache _cache;

    public ScriptAdministrator(
        IAdministratorAccessPolicy policy,
        IScriptRepository scripts,
        IReleaseRepository releases,
        ScriptCache cache)
    {
        _policy = policy;
        _scripts = scripts;
        _releases = releases;
        _cache = cache;
    }

    public Task<IOperationResult<CreateScriptResponse>> CreateScriptAsync(
        RequesterIdentity identity,
        CreateScriptRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => CreateScriptCoreAsync(request, cancellationToken), cancellationToken);

    public Task<IOperationResult<SearchScriptsResponse>> SearchScriptsAsync(
        RequesterIdentity identity,
        SearchScriptsRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => SearchScriptsCoreAsync(request, cancellationToken), cancellationToken);

    public Task<IOperationResult<PublishReleaseResponse>> PublishReleaseAsync(
        RequesterIdentity identity,
        string scriptId,
        PublishReleaseRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => PublishReleaseCoreAsync(scriptId, request, cancellationToken), cancellationToken);

    public Task<IOperationResult<bool>> PromoteReleaseAsync(
        RequesterIdentity identity,
        string scriptId,
        string channelRouteValue,
        PromoteReleaseRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteBoolAsync(
            identity,
            () => PromoteReleaseCoreAsync(scriptId, channelRouteValue, request, cancellationToken),
            cancellationToken);

    public Task<IOperationResult<bool>> AddCustomChannelAsync(
        RequesterIdentity identity,
        string scriptId,
        AddCustomChannelRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteBoolAsync(
            identity,
            () => AddCustomChannelCoreAsync(scriptId, request, cancellationToken),
            cancellationToken);

    public Task<IOperationResult<bool>> DeprecateReleaseAsync(
        RequesterIdentity identity,
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken = default) =>
        ExecuteBoolAsync(
            identity,
            () => DeprecateReleaseCoreAsync(scriptId, releaseId, cancellationToken),
            cancellationToken);

    public Task<IOperationResult<bool>> RestoreReleaseAsync(
        RequesterIdentity identity,
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken = default) =>
        ExecuteBoolAsync(
            identity,
            () => RestoreReleaseCoreAsync(scriptId, releaseId, cancellationToken),
            cancellationToken);

    private async Task<IResult<CreateScriptResponse>> CreateScriptCoreAsync(
        CreateScriptRequest request,
        CancellationToken cancellationToken)
    {
        if (await _scripts.NameExistsAsync(request.Name, cancellationToken))
        {
            return Result<CreateScriptResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.NameAlreadyExists)
                .WithMessage("Já existe um script com este nome.")
                .Build());
        }

        var createResult = Script.Create(
            request.Name,
            request.HostPatterns,
            request.Priority,
            request.Description);

        if (createResult.IsFailure)
            return Result<CreateScriptResponse>.Failure(createResult.Errors);

        var created = await _scripts.InsertAsync(createResult.Value!, cancellationToken);

        return Result<CreateScriptResponse>.Success(new CreateScriptResponse
        {
            Id = created.Id,
        });
    }

    private async Task<IResult<SearchScriptsResponse>> SearchScriptsCoreAsync(
        SearchScriptsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Limit <= 0 || request.Limit > MaxSearchLimit)
        {
            return Result<SearchScriptsResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.SearchLimitInvalid)
                .WithMessage($"O limite de busca deve estar entre 1 e {MaxSearchLimit}.")
                .Build());
        }

        if (request.Offset < 0)
        {
            return Result<SearchScriptsResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.SearchOffsetInvalid)
                .WithMessage("O offset de busca não pode ser negativo.")
                .Build());
        }

        if (request.Keyword?.Length > MaxSearchKeywordLength)
        {
            return Result<SearchScriptsResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.SearchKeywordTooLong)
                .WithMessage($"A palavra-chave não pode exceder {MaxSearchKeywordLength} caracteres.")
                .Build());
        }

        var (items, total) = await _scripts.SearchAsync(
            request.Keyword,
            request.Offset,
            request.Limit,
            cancellationToken);

        return Result<SearchScriptsResponse>.Success(new SearchScriptsResponse
        {
            Offset = request.Offset,
            Limit = request.Limit,
            Total = total,
            Items = items.Select(ToSummary).ToList(),
        });
    }

    private async Task<IResult<PublishReleaseResponse>> PublishReleaseCoreAsync(
        string scriptId,
        PublishReleaseRequest request,
        CancellationToken cancellationToken)
    {
        var script = await _scripts.GetByIdAsync(scriptId, cancellationToken);
        if (script is null)
        {
            return Result<PublishReleaseResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ScriptNotFound)
                .WithMessage("Script não encontrado.")
                .Build());
        }

        var version = await ResolveVersionAsync(scriptId, request, cancellationToken);
        if (version is null)
        {
            return Result<PublishReleaseResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.VersionInvalid)
                .WithMessage("A versão informada é inválida ou já existe.")
                .Build());
        }

        var publishResult = Release.Publish(scriptId, request.SourceCode, version);
        if (publishResult.IsFailure)
            return Result<PublishReleaseResponse>.Failure(publishResult.Errors);

        var created = await _releases.InsertAsync(publishResult.Value!, cancellationToken);

        return Result<PublishReleaseResponse>.Success(new PublishReleaseResponse
        {
            Id = created.Id,
            Version = created.Version.ToString(),
            Hash = created.Hash.Value,
        });
    }

    private async Task<IResult> PromoteReleaseCoreAsync(
        string scriptId,
        string channelRouteValue,
        PromoteReleaseRequest request,
        CancellationToken cancellationToken)
    {
        var script = await _scripts.GetByIdAsync(scriptId, cancellationToken);
        if (script is null)
        {
            return Result.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ScriptNotFound)
                .WithMessage("Script não encontrado.")
                .Build());
        }

        var channelKeyResult = ChannelKey.Parse(channelRouteValue);
        if (channelKeyResult.IsFailure)
            return Result.Failure(channelKeyResult.Errors);

        var release = await _releases.GetByIdAsync(request.ReleaseId, cancellationToken);
        if (release is null || !string.Equals(release.ScriptId, scriptId, StringComparison.Ordinal))
        {
            return Result.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ReleaseNotFound)
                .WithMessage("Release não encontrado para este script.")
                .Build());
        }

        var promoteResult = script.Promote(channelKeyResult.Value!, request.ReleaseId);
        if (promoteResult.IsFailure)
            return promoteResult;

        await _scripts.UpdateAsync(script, cancellationToken);
        _cache.InvalidateAll();
        return Result.Success();
    }

    private async Task<IResult> AddCustomChannelCoreAsync(
        string scriptId,
        AddCustomChannelRequest request,
        CancellationToken cancellationToken)
    {
        var script = await _scripts.GetByIdAsync(scriptId, cancellationToken);
        if (script is null)
        {
            return Result.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ScriptNotFound)
                .WithMessage("Script não encontrado.")
                .Build());
        }

        var addResult = script.AddCustomChannel(request.CustomName);
        if (addResult.IsFailure)
            return addResult;

        await _scripts.UpdateAsync(script, cancellationToken);
        _cache.InvalidateAll();
        return Result.Success();
    }

    private async Task<IResult> DeprecateReleaseCoreAsync(
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken)
    {
        var release = await GetOwnedReleaseAsync(scriptId, releaseId, cancellationToken);
        if (release is null)
        {
            return Result.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ReleaseNotFound)
                .WithMessage("Release não encontrado para este script.")
                .Build());
        }

        var deprecated = release.Deprecate();
        if (deprecated.IsFailure)
            return Result.Failure(deprecated.Errors);

        await _releases.UpdateAsync(deprecated.Value!, cancellationToken);
        _cache.InvalidateAll();
        return Result.Success();
    }

    private async Task<IResult> RestoreReleaseCoreAsync(
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken)
    {
        var release = await GetOwnedReleaseAsync(scriptId, releaseId, cancellationToken);
        if (release is null)
        {
            return Result.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ReleaseNotFound)
                .WithMessage("Release não encontrado para este script.")
                .Build());
        }

        var restored = release.Restore();
        if (restored.IsFailure)
            return Result.Failure(restored.Errors);

        await _releases.UpdateAsync(restored.Value!, cancellationToken);
        _cache.InvalidateAll();
        return Result.Success();
    }

    private async Task<Release?> GetOwnedReleaseAsync(
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken)
    {
        var release = await _releases.GetByIdAsync(releaseId, cancellationToken);

        if (release is null || !string.Equals(release.ScriptId, scriptId, StringComparison.Ordinal))
            return null;

        return release;
    }

    private async Task<SemanticVersion?> ResolveVersionAsync(
        string scriptId,
        PublishReleaseRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Major.HasValue || request.Minor.HasValue || request.Patch.HasValue)
        {
            if (!request.Major.HasValue || !request.Minor.HasValue || !request.Patch.HasValue)
                return null;

            var explicitVersion = new SemanticVersion(request.Major.Value, request.Minor.Value, request.Patch.Value);

            if (await _releases.VersionExistsAsync(scriptId, explicitVersion, cancellationToken))
                return null;

            return explicitVersion;
        }

        var latest = await _releases.GetLatestByScriptIdAsync(scriptId, cancellationToken);
        return latest?.Version.NextPatch() ?? new SemanticVersion(0, 0, 1);
    }

    private static ScriptSummary ToSummary(Script script) =>
        new()
        {
            Id = script.Id,
            Name = script.Name.Value,
            HostPatterns = script.Scope?.Patterns.Select(pattern => pattern.Value).ToArray() ?? Array.Empty<string>(),
            Priority = script.Priority,
            Description = script.Description,
            CreatedAt = script.CreatedAt,
            UpdatedAt = script.UpdatedAt,
        };

    private async Task<IOperationResult<T>> ExecuteAsync<T>(
        RequesterIdentity identity,
        Func<Task<IResult<T>>> executeAsync,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<T>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<T>.Unauthorized(authorization.AuthorizationErrors);

        var result = await executeAsync();

        if (result.IsFailure)
            return OperationResult<T>.Failure(result.Errors);

        if (result.Value is not T value)
            return OperationResult<T>.Failure(result.Errors);

        return OperationResult<T>.Success(value);
    }

    private async Task<IOperationResult<bool>> ExecuteBoolAsync(
        RequesterIdentity identity,
        Func<Task<IResult>> executeAsync,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<bool>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<bool>.Unauthorized(authorization.AuthorizationErrors);

        var result = await executeAsync();

        return result.IsFailure
            ? OperationResult<bool>.Failure(result.Errors)
            : OperationResult<bool>.Success(true);
    }
}
