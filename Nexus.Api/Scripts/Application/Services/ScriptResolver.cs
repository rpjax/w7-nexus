using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Scripts.Aggregates;
using Nexus.Scripts.Application.Contracts;
using Nexus.Scripts.Application.Requests;
using Nexus.Scripts.Application.Responses;
using Nexus.Scripts.Errors;

namespace Nexus.Scripts.Application.Services;

public sealed class ScriptResolver : IScriptResolver
{
    private readonly IScriptRepository _scripts;
    private readonly IReleaseRepository _releases;
    private readonly ScriptCache _cache;

    public ScriptResolver(IScriptRepository scripts, IReleaseRepository releases, ScriptCache cache)
    {
        _scripts = scripts;
        _releases = releases;
        _cache = cache;
    }

    public async Task<IResult<ResolveScriptsResponse>> ResolveAsync(
        ResolveScriptsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Host) && string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<ResolveScriptsResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ResolveHostOrNameRequired)
                .WithMessage("Informe host ou name para resolver scripts.")
                .Build());
        }

        if (!string.IsNullOrWhiteSpace(request.Version) && !SemanticVersion.TryParse(request.Version, out _))
        {
            return Result<ResolveScriptsResponse>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.VersionInvalid)
                .WithMessage("A versão informada é inválida.")
                .Build());
        }

        var channelResult = ChannelKey.Parse(request.Channel);
        if (channelResult.IsFailure)
            return Result<ResolveScriptsResponse>.Failure(channelResult.Errors);

        var channelKey = channelResult.Value!;
        var cacheKey = ScriptCache.BuildCacheKey(request, channelKey.ToRouteValue());

        var response = await _cache.GetOrCreateAsync(
            cacheKey,
            () => BuildResponseAsync(request, channelKey, cancellationToken));

        return Result<ResolveScriptsResponse>.Success(response);
    }

    private async Task<ResolveScriptsResponse> BuildResponseAsync(
        ResolveScriptsRequest request,
        ChannelKey channelKey,
        CancellationToken cancellationToken)
    {
        var items = string.IsNullOrWhiteSpace(request.Name)
            ? await ResolveByHostAsync(request.Host!, channelKey, request, cancellationToken)
            : await ResolveByNameAsync(request.Name, request.Host, channelKey, request, cancellationToken);

        return new ResolveScriptsResponse
        {
            Items = items,
            AggregateHash = ScriptCache.ComputeAggregateHash(items),
        };
    }

    private async Task<List<ResolvedScriptItem>> ResolveByNameAsync(
        string name,
        string? host,
        ChannelKey channelKey,
        ResolveScriptsRequest request,
        CancellationToken cancellationToken)
    {
        var script = await _scripts.GetByNameAsync(name, cancellationToken);
        if (script is null)
            return [];

        if (!string.IsNullOrWhiteSpace(host)
            && script.HasHostPatterns()
            && !script.MatchesHost(host))
            return [];

        return await ResolveScriptItemsAsync([script], channelKey, request, cancellationToken);
    }

    private async Task<List<ResolvedScriptItem>> ResolveByHostAsync(
        string host,
        ChannelKey channelKey,
        ResolveScriptsRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedHost = HostPattern.NormalizeHost(host);
        var scripts = await _scripts.ListWithHostPatternsAsync(cancellationToken);
        var matches = scripts
            .Where(script => script.MatchesHost(normalizedHost))
            .OrderBy(script => script.Priority)
            .ThenBy(script => script.Name.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return await ResolveScriptItemsAsync(matches, channelKey, request, cancellationToken);
    }

    private async Task<List<ResolvedScriptItem>> ResolveScriptItemsAsync(
        IReadOnlyList<Script> scripts,
        ChannelKey channelKey,
        ResolveScriptsRequest request,
        CancellationToken cancellationToken)
    {
        if (scripts.Count == 0)
            return [];

        SemanticVersion? explicitVersion = null;

        if (!string.IsNullOrWhiteSpace(request.Version))
            SemanticVersion.TryParse(request.Version, out explicitVersion);

        var items = new List<ResolvedScriptItem>();

        if (explicitVersion is not null)
        {
            var scriptIds = scripts.Select(script => script.Id).ToList();
            var releases = await _releases.GetByScriptIdsAndVersionAsync(scriptIds, explicitVersion, cancellationToken);
            var releasesByScriptId = releases.ToDictionary(release => release.ScriptId, StringComparer.Ordinal);

            foreach (var script in scripts)
            {
                if (!releasesByScriptId.TryGetValue(script.Id, out var release))
                    continue;

                if (!request.AllowDeprecated && release.IsDeprecated)
                    continue;

                items.Add(ToResolvedItem(script, release));
            }

            return items;
        }

        var releaseIds = scripts
            .Select(script => script.FindChannel(channelKey)?.CurrentReleaseId)
            .Where(releaseId => !string.IsNullOrWhiteSpace(releaseId))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var channelReleases = await _releases.GetByIdsAsync(releaseIds, cancellationToken);
        var releasesById = channelReleases.ToDictionary(release => release.Id, StringComparer.Ordinal);

        foreach (var script in scripts)
        {
            var releaseId = script.FindChannel(channelKey)?.CurrentReleaseId;
            if (releaseId is null || !releasesById.TryGetValue(releaseId, out var release))
                continue;

            if (!request.AllowDeprecated && release.IsDeprecated)
                continue;

            items.Add(ToResolvedItem(script, release));
        }

        return items;
    }

    private static ResolvedScriptItem ToResolvedItem(Script script, Release release) =>
        new()
        {
            Name = script.Name.Value,
            Version = release.Version.ToString(),
            Hash = release.Hash.Value,
            SourceCode = release.SourceCode,
            Priority = script.Priority,
        };
}
