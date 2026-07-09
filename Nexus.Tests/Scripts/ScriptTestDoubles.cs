using Nexus.Scripts.Aggregates;
using Nexus.Scripts.Application.Contracts;

namespace Nexus.Tests.Scripts;

public sealed class InMemoryScriptRepository : IScriptRepository
{
    private readonly List<Script> _scripts = new();

    public Task<Script?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_scripts.FirstOrDefault(script => script.Id == id));

    public Task<Script?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(_scripts.FirstOrDefault(script =>
            string.Equals(script.Name.Value, name, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Script>> ListWithHostPatternsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Script>>(_scripts.Where(script => script.HasHostPatterns()).ToList());

    public Task<(IReadOnlyList<Script> Items, int Total)> SearchAsync(
        string? keyword,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _scripts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(script =>
                script.Name.Value.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || (script.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var materialized = query.OrderBy(script => script.Name.Value).ToList();
        var items = materialized.Skip(offset).Take(limit).ToList();
        return Task.FromResult<(IReadOnlyList<Script>, int)>((items, materialized.Count));
    }

    public Task<Script> InsertAsync(Script script, CancellationToken cancellationToken = default)
    {
        var stored = CloneWithId(script, string.IsNullOrWhiteSpace(script.Id) ? Guid.NewGuid().ToString("N") : script.Id);
        _scripts.Add(stored);
        return Task.FromResult(stored);
    }

    public Task UpdateAsync(Script script, CancellationToken cancellationToken = default)
    {
        var index = _scripts.FindIndex(item => item.Id == script.Id);
        if (index >= 0)
            _scripts[index] = script;

        return Task.CompletedTask;
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(_scripts.Any(script =>
            string.Equals(script.Name.Value, name, StringComparison.OrdinalIgnoreCase)));

    private static Script CloneWithId(Script script, string id) =>
        new(
            id,
            script.Name,
            script.Scope,
            script.Priority,
            script.Description,
            script.Channels,
            script.CreatedAt,
            script.UpdatedAt);
}

public sealed class InMemoryReleaseRepository : IReleaseRepository
{
    private readonly List<Release> _releases = new();

    public Task<Release?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_releases.FirstOrDefault(release => release.Id == id));

    public Task<IReadOnlyList<Release>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var set = ids.ToHashSet(StringComparer.Ordinal);
        return Task.FromResult<IReadOnlyList<Release>>(_releases.Where(release => set.Contains(release.Id)).ToList());
    }

    public Task<IReadOnlyList<Release>> GetByScriptIdsAndVersionAsync(
        IEnumerable<string> scriptIds,
        SemanticVersion version,
        CancellationToken cancellationToken = default)
    {
        var set = scriptIds.ToHashSet(StringComparer.Ordinal);
        return Task.FromResult<IReadOnlyList<Release>>(_releases
            .Where(release => set.Contains(release.ScriptId) && release.Version.Equals(version))
            .ToList());
    }

    public Task<Release?> GetLatestByScriptIdAsync(string scriptId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_releases
            .Where(release => release.ScriptId == scriptId)
            .OrderByDescending(release => release.Version)
            .FirstOrDefault());

    public Task<IReadOnlyList<Release>> ListByScriptIdAsync(string scriptId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Release>>(_releases
            .Where(release => release.ScriptId == scriptId)
            .OrderByDescending(release => release.Version)
            .ToList());

    public Task<Release> InsertAsync(Release release, CancellationToken cancellationToken = default)
    {
        var stored = Clone(release, string.IsNullOrWhiteSpace(release.Id) ? Guid.NewGuid().ToString("N") : release.Id);
        _releases.Add(stored);
        return Task.FromResult(stored);
    }

    public Task UpdateAsync(Release release, CancellationToken cancellationToken = default)
    {
        var index = _releases.FindIndex(item => item.Id == release.Id);
        if (index >= 0)
            _releases[index] = release;

        return Task.CompletedTask;
    }

    public Task<bool> VersionExistsAsync(string scriptId, SemanticVersion version, CancellationToken cancellationToken = default) =>
        Task.FromResult(_releases.Any(release =>
            release.ScriptId == scriptId && release.Version.Equals(version)));

    private static Release Clone(Release release, string id) =>
        new(
            id,
            release.ScriptId,
            release.Version,
            release.SourceCode,
            release.Hash,
            release.CreatedAt,
            release.IsDeprecated);
}
