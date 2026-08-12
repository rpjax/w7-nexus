using Refactor.Nexus.Api.Journal.Models;

namespace Refactor.Nexus.Api.Journal.Services.Contracts;

/// <summary>
/// Durable store seam for Journal drain and reads.
/// </summary>
public interface IJournalRepository
{
    /// <summary>
    /// Persists a batch atomically. Skips Ids that already exist (idempotent retry).
    /// Returns the number of newly inserted rows.
    /// </summary>
    Task<int> SaveBatchAsync(
        IReadOnlyList<JournalEntry> entries,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads persisted entries matching <paramref name="query"/> (envelope/index filters only).
    /// </summary>
    Task<IReadOnlyList<JournalEntry>> ReadAsync(
        JournalQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Approximate durable journal payload bytes (payload lengths + envelope overhead).</summary>
    Task<long> EstimateStoredBytesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes oldest entries matching optional type / index-key filters published before
    /// <paramref name="olderThan"/>. Foundation retention primitive — callers compose policies.
    /// </summary>
    Task<int> DeleteOlderThanAsync(
        DateTimeOffset olderThan,
        int take,
        string? type = null,
        string? requireIndexKeyType = null,
        string? excludeIndexKeyType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes every entry carrying the given index key (any age).
    /// Use sparingly — cascades across all fact types sharing that key.
    /// </summary>
    Task<int> DeleteByIndexKeyAsync(
        string indexKeyType,
        string indexKeyValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes facts that do NOT carry <paramref name="excludeIndexKeyType"/> (when set),
    /// optionally narrowed by fact <paramref name="type"/> and/or <paramref name="olderThan"/>.
    /// </summary>
    Task<int> DeleteIndependentFactsAsync(
        string? type,
        DateTimeOffset? olderThan,
        string? excludeIndexKeyType = null,
        CancellationToken cancellationToken = default);

    /// <summary>Count of facts carrying the given index key (dry-run / summary display).</summary>
    Task<int> CountByIndexKeyAsync(
        string indexKeyType,
        string indexKeyValue,
        CancellationToken cancellationToken = default);

    /// <summary>Count of facts eligible for <see cref="DeleteIndependentFactsAsync"/>.</summary>
    Task<int> CountIndependentFactsAsync(
        string? type,
        DateTimeOffset? olderThan,
        string? excludeIndexKeyType = null,
        CancellationToken cancellationToken = default);
}
