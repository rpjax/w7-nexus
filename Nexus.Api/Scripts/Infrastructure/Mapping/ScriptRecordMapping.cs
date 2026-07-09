using MongoDB.Bson;
using Nexus.Database.Models;
using Nexus.Scripts.Aggregates;

namespace Nexus.Scripts.Infrastructure.Mapping;

internal static class ScriptRecordMapping
{
    public static Script ToScript(ScriptRecord record)
    {
        var name = ScriptName.Create(record.Name).Value!;
        DeploymentScope? scope = null;

        if (record.HostPatterns.Count > 0)
            scope = DeploymentScope.Create(record.HostPatterns).Value;

        var channels = record.Channels
            .Select(ToChannel)
            .ToList();

        return new Script(
            record.Id.ToString(),
            name,
            scope,
            record.Priority,
            record.Description,
            channels,
            record.CreatedAt,
            record.UpdatedAt);
    }

    public static ScriptRecord ToRecord(Script script)
    {
        var record = new ScriptRecord
        {
            Id = string.IsNullOrWhiteSpace(script.Id)
                ? ObjectId.GenerateNewId()
                : ObjectId.Parse(script.Id),
            Name = script.Name.Value,
            HostPatterns = script.Scope?.Patterns.Select(pattern => pattern.Value).ToList() ?? new List<string>(),
            Priority = script.Priority,
            Description = script.Description,
            Channels = script.Channels.Select(ToChannelRecord).ToList(),
            CreatedAt = script.CreatedAt,
            UpdatedAt = script.UpdatedAt,
        };

        return record;
    }

    private static Channel ToChannel(ChannelRecord record)
    {
        var key = ChannelKey.Create(record.Type, record.CustomName).Value!;
        return new Channel(
            record.Id.ToString(),
            key,
            record.CurrentReleaseId);
    }

    private static ChannelRecord ToChannelRecord(Channel channel) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(channel.Id)
                ? ObjectId.GenerateNewId()
                : ObjectId.Parse(channel.Id),
            Type = channel.Key.Type,
            CustomName = channel.Key.CustomName,
            CurrentReleaseId = channel.CurrentReleaseId,
        };
}
