using Refactor.Nexus.Api.Journal.Models;

namespace Refactor.Nexus.Api.Journal.Services;

/// <summary>
/// Outcome of drain policy for one batch.
/// </summary>
public sealed class JournalDrainDecision
{
    public required IReadOnlyList<JournalEntry> Persist { get; init; }
    public required IReadOnlyList<JournalEntry> Drop { get; init; }
}
