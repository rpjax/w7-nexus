# Journal

Unified **operational fact log** for Nexus — foundation for the future Nexus logging / audit narrative.

Journal answers: *what happened, when, and which entities were involved?*  
It does **not** answer: *what is the current aggregate state?*

| Journal is | Journal is not |
|------------|----------------|
| Append-only operational narrative | Event-sourcing / aggregate rebuild |
| Searchable by envelope + index keys | A substitute for ASP.NET / ILogger diagnostics |
| Cheap sync admission, async durable drain | A caller-side “await until on disk” API |
| Schema-governed payloads (`Type` + `SchemaVersion`) | Free-form log lines |

**Hard rule:** never rebuild domain aggregates from Journal rows.

Fact types are toggled in the Journal catalog; health is Journal-local (`Healthy` / `Degraded`).

`PublishPolicy.Guaranteed` means *persist at least once while the process is alive and the sink is healthy* — not “on disk when Append returns,” and not crash-proof across process death before flush.

---

## Lifecycle

```text
Boot
  AddRefactorAccounts() → shared INpgsqlConnectionFactory (Postgres)
  AddJournal() → JournalDbContext on same connection + admission / drain
  DiscoverJournalFacts() → scan [JournalFact] in host assembly (idempotent)
  InitializeJournalDatabaseAsync() → CREATE TABLE IF NOT EXISTS journal_*
  JournalWorker (IHostedService) starts drain

Append
  writer.Append(payload)
    → catalog + enablement (admission-time only)
    → stamp Id/PublishedAt, indexes, JSON (payload size / column limits)
    → queue.Enqueue (depth reserve → TryWrite; Soft/Hard/Max guards)

Drain (JournalWorker)
  supervised: await LoopAsync (TakeBatch → ProcessBatch)
  on crash → record → if budget exceeded StopApplication; else backoff + restart

Shutdown
  Cancel TakeBatch wait → finish in-flight persist → TakeBatch remainder while Count>0 (ShutdownFlushTimeout)
  Append rejected when admission closed (drain stopped / crash backoff)
```

`Append` success means *accepted into the admission channel* (or silently skipped when disabled), not *on disk*.

---

## Admission matrix

| Condition | BestEffort | Guaranteed |
|-----------|------------|------------|
| Unregistered + `RejectUnregisteredTypes` | throw | throw |
| Unregistered + reject false | skip (metric) | skip (metric) |
| Type disabled in catalog | skip (metric) | skip (metric) |
| `depth >= Soft` (Soft&gt;0) | drop | admit |
| `depth >= Hard` (Hard&gt;0) | drop | admit + queue-pressure Degraded (rising edge) |
| `depth >= Max` (Max&gt;0) | drop | reject + persist Degraded |
| Payload / index over limits | throw | throw |
| `TryWrite` fails | drop | fail + persist Degraded |

Enablement is evaluated at Append time only — disabling a type does not remove already-queued facts.

---

## Drain stack

| Piece | Role |
|-------|------|
| `IJournalQueue` / `JournalQueue` | `Enqueue`/`Count` + one blocking `TakeBatchAsync` (CT abort); channel never completes |
| `IJournalDrainPolicy` | Guaranteed before BestEffort; under Degraded drop BestEffort (keep ≤ `DegradedBestEffortKeep`) |
| `IJournalHealth` | Persist Degraded (sticky) **or** queue pressure (clears when depth falls) |
| `JournalWorker` | `IHostedService`: await batch → persist with in-place retries; crash supervisor |
| `IJournalRepository` | SaveBatch + Read + retention primitives (single store port) |
| `JournalDrainMetrics` | In-process counters + `System.Diagnostics.Metrics` (`Nexus.Journal`) + `ActivitySource` |
| `JournalHealthCheck` | Host health check tag `journal` |

### Depth guards

| Setting | Behavior |
|---------|----------|
| `SoftQueueDepth` (&gt;0) | Drop BestEffort |
| `SoftQueueDepth` = 0 | Soft shedding off |
| `HardQueueDepth` (&gt;0) | Drop BestEffort + queue-pressure Degraded (clears when depth &lt; Soft, or Hard/2 if Soft=0) |
| `MaxQueueDepth` (&gt;0) | Absolute ceiling — Guaranteed rejected |
| Guaranteed below Max | Always reserved+`TryWrite` |

### Persist failure

- Persist Degraded → sticky until `inserted > 0` × `RecoverAfterSuccessfulBatches` (or `Recover()`)
- Same batch retried in place up to `MaxPersistAttempts`, then dropped (`journal.persist_abandoned`)
- Unique Id conflicts treated as idempotent (0 inserted)
- Crash budget: `MaxCrashesInPeriod` within `CrashPeriod` → `StopApplication` (checked immediately after crash, before backoff)
- Ready health: `Unhealthy` when drain is not running; `Degraded` when persist/queue pressure active

---

## Declaring a fact

Facts live with the owning domain (e.g. `Accounts/.../Journal/AccountCreated.cs`):

```csharp
[JournalFact("Accounts.AccountCreated", schemaVersion: 1,
    Owner = "accounts",
    PublishPolicy = PublishPolicy.Guaranteed)]
public sealed class AccountCreated
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required string Email { get; init; }
}
```

Emit: `writer.Append(new AccountCreated { ... });`

---

## DI

```csharp
builder.Services.AddRefactorAccounts(builder.Configuration);
builder.Services.AddJournal();
builder.Services.DiscoverJournalFacts();

var app = builder.Build();
await app.Services.InitializeAccountsDatabaseAsync();
await app.Services.InitializeJournalDatabaseAsync();
```

Configuration:

- `ConnectionStrings:AccountsDb` or `NEXUS_ACCOUNTS_DB_CONNECTION` — shared Postgres (same as Accounts)
- `"Journal"` — drain / admission tunables (`JournalDrainOptions`, validated on start)

Ports: `IJournalWriter`, `IJournalReader`, `IJournalCatalog`, `IJournalQueue`, `IJournalLiveFeed`, `IJournalHealth`, `IJournalRepository`, `IJournalDrainPolicy`, `JournalDrainMetrics`. Hosted: `JournalWorker` (`IHostedService`).

Store: [`JournalDbContext`](Storage/JournalDbContext.cs) on shared Postgres; schema via [`JournalDatabaseInitializer`](Storage/JournalDatabaseInitializer.cs) (`journal_entries`, `journal_index_keys`).

---

## Query

`IJournalReader.ReadAsync(JournalQuery?)` — envelope/index filters (including `PublishPolicy`), orders (`Sequence`, `PublishedAt`, `IndexKeyType`).  
Default `Limit` = `DefaultReadLimit` (1000); hard ceiling `MaxReadLimit` (10_000). Not payload JSON paths.

---

## Live observation

`IJournalLiveFeed` fans admitted facts out in-process, after the catalog enablement
gate and right after enqueue. It is an observation tap, never a read model:

- No replay — a subscriber sees only facts admitted while it is subscribed.
- No back-pressure on admission — each subscriber has a bounded buffer
  (`JournalLiveFeed.SubscriberCapacity`) that drops its oldest fact when full.
- `Sequence` is still zero: durable order comes from `IJournalReader`.

Presentation (hubs / admin HTTP) is out of scope for this foundation slice.

---

## Folder map

```text
Journal/
  Attributes/    [JournalFact], [JournalIndex]
  Catalog/       Descriptor + factory
  Composition/   AddJournal / DiscoverJournalFacts
  Models/        Envelope, PublishPolicy, Query, HealthState
  Services/      Writer, Catalog, Queue, LiveFeed, Worker, Policy, Health, Metrics, Reader, Options
  Storage/       JournalDbContext, initializer, EF configs, Repository, records, mapper
  JournalFactDiscovery.cs
  README.md
```

Namespaces: `Refactor.Nexus.Api.Journal.*`

---

## Status / out of scope

**In place:** admission matrix, Channel queue, drain worker, EF/Postgres on shared Nexus DB, dual-factor health, meters/activities, health check, options validation, `DiscoverJournalFacts`, generalized retention primitives.

**Not wired yet:** `Program.cs` enablement, domain fact types, Admin HTTP recover/metrics UI, retention cleaner host, EF migrations.

---

## Anti-patterns

- Rebuilding aggregates from Journal history  
- Awaiting store I/O inside `Append`  
- Soft-skipping missing required indexes  
- Treating Journal as a second ILogger sink  
- A separate Journal database  
- Public queue delete/reorder/purge APIs  
- Completing / closing the Journal channel (it is process-lifetime)  
- Polling the queue instead of Channel wake  
- Treating Guaranteed as crash-proof across process death  
