using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Journal.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Journal.Domain.Errors;
using Refactor.Nexus.Api.Journal.Models;
using Refactor.Nexus.Api.Journal.Services.Contracts;

namespace Refactor.Nexus.Api.Journal.Application.UseCases.Administrator.Queries;

public sealed record JournalEntryView(
    Guid Id,
    long Sequence,
    string Type,
    int SchemaVersion,
    DateTimeOffset PublishedAt,
    IReadOnlyList<JournalIndexKeyView> IndexKeys);

public sealed record JournalIndexKeyView(string Type, string Value);

public sealed record ListJournalEntriesQuery(int? Limit, int Offset, string? Type);
public sealed record ListJournalEntriesResult(IReadOnlyList<JournalEntryView> Items);

public interface IListJournalEntriesUseCase
{
    Task<IOperationResult<ListJournalEntriesResult>> HandleAsync(
        ListJournalEntriesQuery query,
        CancellationToken cancellationToken = default);
}

public sealed class ListJournalEntriesHandler : IListJournalEntriesUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IJournalAccess _access;
    private readonly IJournalReader _reader;

    public ListJournalEntriesHandler(
        IRequestContext requestContext,
        IJournalAccess access,
        IJournalReader reader)
    {
        _requestContext = requestContext;
        _access = access;
        _reader = reader;
    }

    public async Task<IOperationResult<ListJournalEntriesResult>> HandleAsync(
        ListJournalEntriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<ListJournalEntriesResult>.Failure(requesterResult.Errors);

        if (!Guid.TryParse(requester.AccountId, out var accountId))
            return OperationResult<ListJournalEntriesResult>.Unauthorized(Unauthorized("Identidade invalida."));

        var allowed = await _access.CanReadAuditLogAsync(accountId, cancellationToken)
            || requester.Roles.Contains(Roles.Administrator, StringComparer.OrdinalIgnoreCase);
        if (!allowed)
            return OperationResult<ListJournalEntriesResult>.Unauthorized(Unauthorized("Requer Admin ou ler_log_auditoria."));

        var filter = string.IsNullOrWhiteSpace(query.Type)
            ? null
            : new JournalQueryFilter { Type = query.Type.Trim() };

        var entries = await _reader.ReadAsync(
            new JournalQuery
            {
                Limit = query.Limit,
                Offset = query.Offset,
                Filter = filter
            },
            cancellationToken);

        return OperationResult<ListJournalEntriesResult>.Success(
            new ListJournalEntriesResult(entries.Select(ToView).ToList()));
    }

    private static JournalEntryView ToView(JournalEntry entry) =>
        new(
            entry.Id,
            entry.Sequence,
            entry.Type,
            entry.SchemaVersion,
            entry.PublishedAt,
            entry.IndexKeys.Select(k => new JournalIndexKeyView(k.Type, k.Value)).ToList());

    private static Error Unauthorized(string message) =>
        Error.Create().WithCode(JournalErrorCodes.Unauthorized).WithMessage(message).Build();
}
