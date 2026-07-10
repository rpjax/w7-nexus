using Nexus.Scripts.Aggregates;

namespace Nexus.Scripts.Application.Contracts;

public interface IReleaseRepository
{
    Task<Release?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Release>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Release>> GetByScriptIdsAndVersionAsync(
        IEnumerable<string> scriptIds,
        SemanticVersion version,
        CancellationToken cancellationToken = default);
    Task<Release?> GetLatestByScriptIdAsync(string scriptId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Release>> ListByScriptIdAsync(string scriptId, CancellationToken cancellationToken = default);
    Task<Release> InsertAsync(Release release, CancellationToken cancellationToken = default);
    Task UpdateAsync(Release release, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> VersionExistsAsync(string scriptId, SemanticVersion version, CancellationToken cancellationToken = default);
}
