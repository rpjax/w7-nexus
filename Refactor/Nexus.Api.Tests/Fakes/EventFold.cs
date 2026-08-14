namespace Refactor.Nexus.Api.Tests.Fakes;

internal static class EventFold
{
    public static T Replay<T>(IEnumerable<object> events) where T : new()
    {
        dynamic aggregate = new T();
        foreach (var @event in events)
            aggregate.Apply((dynamic)@event);

        return aggregate;
    }
}

internal sealed class EventStreamBag
{
    private readonly Dictionary<Guid, List<object>> _streams = [];

    public void Append(Guid streamId, IEnumerable<object> events)
    {
        if (!_streams.TryGetValue(streamId, out var list))
        {
            list = [];
            _streams[streamId] = list;
        }

        list.AddRange(events.ToArray());
    }

    public IReadOnlyList<object>? Get(Guid streamId) =>
        _streams.TryGetValue(streamId, out var list) ? list : null;

    public IEnumerable<Guid> StreamIds => _streams.Keys;

    public T? Load<T>(Guid streamId) where T : new()
    {
        var events = Get(streamId);
        return events is null ? default : EventFold.Replay<T>(events);
    }
}
