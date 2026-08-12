using Refactor.Nexus.Api.Journal.Models;

namespace Refactor.Nexus.Api.Journal.Services.Contracts;

public interface IJournalDrainPolicy
{
    JournalDrainDecision Decide(
        IReadOnlyList<JournalEntry> batch,
        JournalHealthState health,
        JournalDrainOptions options);
}
