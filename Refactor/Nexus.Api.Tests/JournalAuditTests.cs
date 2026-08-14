using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Journal.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Journal.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.Journal.Domain.Errors;
using Refactor.Nexus.Api.Journal.Models;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;

namespace Refactor.Nexus.Api.Tests;

public sealed class JournalAuditTests
{
    [Fact]
    public void Ler_log_auditoria_is_a_known_capability() =>
        Assert.True(Capabilities.IsKnown(Capabilities.LerLogAuditoria));

    [Fact]
    public async Task Admin_can_list_journal_entries_without_payload()
    {
        var entry = SampleEntry();
        var handler = new ListJournalEntriesHandler(
            new StaticRequestContext(Guid.NewGuid().ToString(), [Roles.Administrator], []),
            new DenyAccess(),
            new StubReader([entry]));

        var result = await handler.HandleAsync(new ListJournalEntriesQuery(null, 0, null));

        Assert.False(result.IsFailure);
        var view = Assert.Single(result.Value!.Items);
        Assert.Equal(entry.Id, view.Id);
        Assert.Equal(entry.Type, view.Type);
        Assert.DoesNotContain("Payload", typeof(JournalEntryView).GetProperties().Select(p => p.Name));
    }

    [Fact]
    public async Task Capability_without_admin_role_can_list()
    {
        var handler = new ListJournalEntriesHandler(
            new StaticRequestContext(Guid.NewGuid().ToString(), [], []),
            new AllowAccess(),
            new StubReader([SampleEntry()]));

        var result = await handler.HandleAsync(new ListJournalEntriesQuery(10, 0, "Ledger.HopRegistered"));
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
    }

    [Fact]
    public async Task Missing_capability_and_role_is_unauthorized()
    {
        var handler = new ListJournalEntriesHandler(
            new StaticRequestContext(Guid.NewGuid().ToString(), [], []),
            new DenyAccess(),
            new StubReader([SampleEntry()]));

        var result = await handler.HandleAsync(new ListJournalEntriesQuery(null, 0, null));
        Assert.True(result.IsFailure);
        Assert.False(result.IsAuthorized);
        Assert.Contains(result.AuthorizationErrors, e => e.Code == JournalErrorCodes.Unauthorized);
    }

    private static JournalEntry SampleEntry() =>
        new()
        {
            Id = Guid.NewGuid(),
            Sequence = 1,
            Type = "Ledger.HopRegistered",
            SchemaVersion = 1,
            PublishedAt = DateTimeOffset.UtcNow,
            IndexKeys = [new JournalIndexKey("hop", Guid.NewGuid().ToString())],
            Payload = "{\"secret\":true}"
        };

    private sealed class StaticRequestContext : IRequestContext
    {
        private readonly RequesterContext _context;
        public StaticRequestContext(string accountId, IReadOnlyList<string> roles, IReadOnlyList<string> permissions) =>
            _context = new RequesterContext(accountId, roles, permissions);

        public Task<IResult<RequesterContext>> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IResult<RequesterContext>>(Result<RequesterContext>.Success(_context));
    }

    private sealed class AllowAccess : IJournalAccess
    {
        public Task<bool> CanReadAuditLogAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class DenyAccess : IJournalAccess
    {
        public Task<bool> CanReadAuditLogAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class StubReader : IJournalReader
    {
        private readonly IReadOnlyList<JournalEntry> _entries;
        public StubReader(IReadOnlyList<JournalEntry> entries) => _entries = entries;

        public Task<IReadOnlyList<JournalEntry>> ReadAsync(
            JournalQuery? query = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_entries);
    }
}
