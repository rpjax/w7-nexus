using Aidan.Core.Patterns;

namespace Nexus.Legacy.Notifier.Application;

public interface INotifier
{
    Task<IResult> NotifyAsync(NotificationRequest request);
}

public class NotificationRequest
{
    public IReadOnlyList<string>? Recipients { get; init; }
    public string? Subject { get; init; }
    public IReadOnlyList<string>? Flags { get; init; }
    public string? Data { get; init; }
}

public class Notification
{
    public string Id { get; init; }
    public IReadOnlyList<string>? Recipients { get; init; }
    public string? Subject { get; init; }
    public IReadOnlyList<string>? Flags { get; init; }
    public string? Data { get; init; }

    public Notification(
        string id,
        IReadOnlyList<string>? recipients,
        string? subject,
        IReadOnlyList<string>? flags,
        string? data)
    {
        Id = id;
        Recipients = recipients;
        Subject = subject;
        Flags = flags;
        Data = data;
    }
}