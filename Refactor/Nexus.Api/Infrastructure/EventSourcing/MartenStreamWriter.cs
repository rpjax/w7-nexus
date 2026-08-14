using Marten;

namespace Refactor.Nexus.Api.Infrastructure.EventSourcing;

public static class MartenStreamWriter
{
    public static async Task QueueAsync(
        IDocumentSession session,
        string streamKey,
        Type aggregateType,
        IReadOnlyList<object> events,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
            return;

        var state = await session.Events.FetchStreamStateAsync(streamKey, cancellationToken);
        var payload = events.ToArray();
        if (state is null)
            session.Events.StartStream(aggregateType, streamKey, payload);
        else
            session.Events.Append(streamKey, payload);
    }

    public static async Task SaveAsync(
        IDocumentSession session,
        string streamKey,
        Type aggregateType,
        IReadOnlyList<object> events,
        CancellationToken cancellationToken)
    {
        await QueueAsync(session, streamKey, aggregateType, events, cancellationToken);
        if (events.Count == 0)
            return;
        await session.SaveChangesAsync(cancellationToken);
    }
}
