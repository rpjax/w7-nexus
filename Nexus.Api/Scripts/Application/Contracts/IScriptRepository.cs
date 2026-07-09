using Nexus.Scripts.Aggregates;

namespace Nexus.Scripts.Application.Contracts;

public interface IScriptRepository
{
    Task<Script?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Script?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Script>> ListWithHostPatternsAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Script> Items, int Total)> SearchAsync(
        string? keyword,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);
    Task<Script> InsertAsync(Script script, CancellationToken cancellationToken = default);
    Task UpdateAsync(Script script, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
}
