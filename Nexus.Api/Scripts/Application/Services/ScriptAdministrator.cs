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

    public Task<IOperationResult<ScriptDetailResponse>> GetScriptAsync(
        RequesterIdentity identity,
        string scriptId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => GetScriptCoreAsync(scriptId, cancellationToken), cancellationToken);

    public Task<IOperationResult<ScriptDetailResponse>> UpdateScriptAsync(
        RequesterIdentity identity,
        string scriptId,
        UpdateScriptRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => UpdateScriptCoreAsync(scriptId, request, cancellationToken), cancellationToken);

    public Task<IOperationResult<ReleaseListResponse>> ListReleasesAsync(
        RequesterIdentity identity,
        string scriptId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => ListReleasesCoreAsync(scriptId, cancellationToken), cancellationToken);

    public Task<IOperationResult<ReleaseDetailResponse>> GetReleaseAsync(
        RequesterIdentity identity,
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => GetReleaseCoreAsync(scriptId, releaseId, cancellationToken), cancellationToken);

    public Task<IOperationResult<ReleaseSourceCodeResponse>> GetReleaseSourceCodeAsync(
        RequesterIdentity identity,
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => GetReleaseSourceCodeCoreAsync(scriptId, releaseId, cancellationToken), cancellationToken);

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

    public Task<IOperationResult<DeleteReleaseResponse>> DeleteReleaseAsync(
        RequesterIdentity identity,
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => DeleteReleaseCoreAsync(scriptId, releaseId, cancellationToken), cancellationToken);

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
            Items = await ToSummariesAsync(items, cancellationToken),
        });
    }

    private async Task<IResult<ScriptDetailResponse>> GetScriptCoreAsync(
        string scriptId,
        CancellationToken cancellationToken)
    {
        var script = await _scripts.GetByIdAsync(scriptId, cancellationToken);
        if (script is null)
        {
            return Result<ScriptDetailResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ScriptNotFound)
                .WithMessage("Script não encontrado.")
                .Build());
        }

        return Result<ScriptDetailResponse>.Success(await ToDetailAsync(script, cancellationToken));
    }

    private async Task<IResult<ScriptDetailResponse>> UpdateScriptCoreAsync(
        string scriptId,
        UpdateScriptRequest request,
        CancellationToken cancellationToken)
    {
        var script = await _scripts.GetByIdAsync(scriptId, cancellationToken);
        if (script is null)
        {
            return Result<ScriptDetailResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ScriptNotFound)
                .WithMessage("Script não encontrado.")
                .Build());
        }

        if (request.Description is not null)
        {
            var descriptionResult = script.UpdateDescription(request.Description);
            if (descriptionResult.IsFailure)
                return Result<ScriptDetailResponse>.Failure(descriptionResult.Errors);
        }

        if (request.HostPatterns is not null)
        {
            var scopeResult = script.UpdateScope(request.HostPatterns);
            if (scopeResult.IsFailure)
                return Result<ScriptDetailResponse>.Failure(scopeResult.Errors);
        }

        if (request.Priority.HasValue)
        {
            var priorityResult = script.UpdatePriority(request.Priority.Value);
            if (priorityResult.IsFailure)
                return Result<ScriptDetailResponse>.Failure(priorityResult.Errors);
        }

        await _scripts.UpdateAsync(script, cancellationToken);
        _cache.InvalidateAll();

        return Result<ScriptDetailResponse>.Success(await ToDetailAsync(script, cancellationToken));
    }

    private async Task<IResult<ReleaseListResponse>> ListReleasesCoreAsync(
        string scriptId,
        CancellationToken cancellationToken)
    {
        var script = await _scripts.GetByIdAsync(scriptId, cancellationToken);
        if (script is null)
        {
            return Result<ReleaseListResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ScriptNotFound)
                .WithMessage("Script não encontrado.")
                .Build());
        }

        var releases = await _releases.ListByScriptIdAsync(scriptId, cancellationToken);

        return Result<ReleaseListResponse>.Success(new ReleaseListResponse
        {
            Items = releases.Select(release => ToReleaseSummary(release, script)).ToList(),
        });
    }

    private async Task<IResult<ReleaseDetailResponse>> GetReleaseCoreAsync(
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken)
    {
        var script = await _scripts.GetByIdAsync(scriptId, cancellationToken);
        if (script is null)
        {
            return Result<ReleaseDetailResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ScriptNotFound)
                .WithMessage("Script não encontrado.")
                .Build());
        }

        var release = await GetOwnedReleaseAsync(scriptId, releaseId, cancellationToken);
        if (release is null)
        {
            return Result<ReleaseDetailResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ReleaseNotFound)
                .WithMessage("Release não encontrado para este script.")
                .Build());
        }

        return Result<ReleaseDetailResponse>.Success(ToReleaseDetail(release, script));
    }

    private async Task<IResult<ReleaseSourceCodeResponse>> GetReleaseSourceCodeCoreAsync(
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken)
    {
        var release = await GetOwnedReleaseAsync(scriptId, releaseId, cancellationToken);
        if (release is null)
        {
            return Result<ReleaseSourceCodeResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ReleaseNotFound)
                .WithMessage("Release não encontrado para este script.")
                .Build());
        }

        return Result<ReleaseSourceCodeResponse>.Success(new ReleaseSourceCodeResponse
        {
            SourceCode = release.SourceCode,
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
            SourceCodeSizeBytes = created.SourceCodeSizeBytes,
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

    private async Task<IResult<DeleteReleaseResponse>> DeleteReleaseCoreAsync(
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken)
    {
        var script = await _scripts.GetByIdAsync(scriptId, cancellationToken);
        if (script is null)
        {
            return Result<DeleteReleaseResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ScriptNotFound)
                .WithMessage("Script não encontrado.")
                .Build());
        }

        var release = await GetOwnedReleaseAsync(scriptId, releaseId, cancellationToken);
        if (release is null)
        {
            return Result<DeleteReleaseResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ReleaseNotFound)
                .WithMessage("Release não encontrado para este script.")
                .Build());
        }

        var clearedChannels = script.ClearReleaseReference(releaseId);
        if (clearedChannels.Count > 0)
            await _scripts.UpdateAsync(script, cancellationToken);

        var deleted = await _releases.DeleteAsync(releaseId, cancellationToken);
        if (!deleted)
        {
            return Result<DeleteReleaseResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ReleaseNotFound)
                .WithMessage("Release não encontrado para este script.")
                .Build());
        }

        _cache.InvalidateAll();

        return Result<DeleteReleaseResponse>.Success(new DeleteReleaseResponse
        {
            ClearedChannelRouteValues = clearedChannels,
        });
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

    private async Task<List<ScriptSummary>> ToSummariesAsync(
        IReadOnlyList<Script> scripts,
        CancellationToken cancellationToken)
    {
        var releaseIds = scripts
            .SelectMany(script => script.Channels.Select(channel => channel.CurrentReleaseId))
            .Where(releaseId => !string.IsNullOrWhiteSpace(releaseId))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var releases = await _releases.GetByIdsAsync(releaseIds, cancellationToken);
        var releasesById = releases.ToDictionary(release => release.Id, StringComparer.Ordinal);

        return scripts.Select(script => new ScriptSummary
        {
            Id = script.Id,
            Name = script.Name.Value,
            HostPatterns = script.Scope?.Patterns.Select(pattern => pattern.Value).ToArray() ?? Array.Empty<string>(),
            Priority = script.Priority,
            Description = script.Description,
            CreatedAt = script.CreatedAt,
            UpdatedAt = script.UpdatedAt,
            Channels = script.Channels.Select(channel => ToChannelSummary(channel, releasesById)).ToList(),
        }).ToList();
    }

    private async Task<ScriptDetailResponse> ToDetailAsync(Script script, CancellationToken cancellationToken)
    {
        var releaseIds = script.Channels
            .Select(channel => channel.CurrentReleaseId)
            .Where(releaseId => !string.IsNullOrWhiteSpace(releaseId))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var releases = await _releases.GetByIdsAsync(releaseIds, cancellationToken);
        var releasesById = releases.ToDictionary(release => release.Id, StringComparer.Ordinal);

        return new ScriptDetailResponse
        {
            Id = script.Id,
            Name = script.Name.Value,
            HostPatterns = script.Scope?.Patterns.Select(pattern => pattern.Value).ToArray() ?? Array.Empty<string>(),
            Priority = script.Priority,
            Description = script.Description,
            CreatedAt = script.CreatedAt,
            UpdatedAt = script.UpdatedAt,
            Channels = script.Channels.Select(channel => ToChannelSummary(channel, releasesById)).ToList(),
        };
    }

    private static ChannelSummary ToChannelSummary(
        Channel channel,
        IReadOnlyDictionary<string, Release> releasesById)
    {
        Release? release = null;

        if (!string.IsNullOrWhiteSpace(channel.CurrentReleaseId))
            releasesById.TryGetValue(channel.CurrentReleaseId, out release);

        return new ChannelSummary
        {
            RouteValue = channel.Key.ToRouteValue(),
            DisplayName = channel.Key.Type switch
            {
                ChannelType.Production => "Production",
                ChannelType.Staging => "Staging",
                ChannelType.Development => "Development",
                ChannelType.Custom => channel.Key.CustomName ?? "Custom",
                _ => channel.Key.ToRouteValue(),
            },
            IsCustom = channel.Key.Type == ChannelType.Custom,
            CurrentReleaseId = channel.CurrentReleaseId,
            Version = release?.Version.ToString(),
            Hash = release?.Hash.Value,
            IsDeprecated = release?.IsDeprecated,
        };
    }

    private static ReleaseSummary ToReleaseSummary(Release release, Script script) =>
        new()
        {
            Id = release.Id,
            Version = release.Version.ToString(),
            Hash = release.Hash.Value,
            SourceCodeSizeBytes = release.SourceCodeSizeBytes,
            IsDeprecated = release.IsDeprecated,
            CreatedAt = release.CreatedAt,
            PromotedChannelRouteValues = script.Channels
                .Where(channel => string.Equals(channel.CurrentReleaseId, release.Id, StringComparison.Ordinal))
                .Select(channel => channel.Key.ToRouteValue())
                .ToList(),
        };

    private static ReleaseDetailResponse ToReleaseDetail(Release release, Script script) =>
        new()
        {
            Id = release.Id,
            ScriptId = release.ScriptId,
            Version = release.Version.ToString(),
            Hash = release.Hash.Value,
            SourceCodeSizeBytes = release.SourceCodeSizeBytes,
            IsDeprecated = release.IsDeprecated,
            CreatedAt = release.CreatedAt,
            PromotedChannelRouteValues = script.Channels
                .Where(channel => string.Equals(channel.CurrentReleaseId, release.Id, StringComparison.Ordinal))
                .Select(channel => channel.Key.ToRouteValue())
                .ToList(),
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
