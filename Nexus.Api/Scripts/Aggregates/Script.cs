using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Scripts.Errors;

namespace Nexus.Scripts.Aggregates;

public sealed class Script
{
    public const int MaxDescriptionLength = 2000;

    public string Id { get; }
    public ScriptName Name { get; }
    public DeploymentScope? Scope { get; private set; }
    public int Priority { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<Channel> _channels;
    public IReadOnlyList<Channel> Channels => _channels.AsReadOnly();

    internal Script(
        string id,
        ScriptName name,
        DeploymentScope? scope,
        int priority,
        string? description,
        IReadOnlyList<Channel> channels,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        Name = name;
        Scope = scope;
        Priority = priority;
        Description = description;
        _channels = channels.ToList();
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static IResult<Script> Create(
        string? name,
        IEnumerable<string>? hostPatterns = null,
        int priority = 0,
        string? description = null)
    {
        var nameResult = ScriptName.Create(name);
        if (nameResult.IsFailure)
            return Result<Script>.Failure(nameResult.Errors);

        description = description?.Trim();

        if (description?.Length > MaxDescriptionLength)
            return Result<Script>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.DescriptionInvalid)
                .WithMessage($"A descrição não pode exceder {MaxDescriptionLength} caracteres.")
                .Build());

        DeploymentScope? scope = null;
        var patterns = hostPatterns?.ToArray() ?? Array.Empty<string>();

        if (patterns.Length > 0)
        {
            var scopeResult = DeploymentScope.Create(patterns);
            if (scopeResult.IsFailure)
                return Result<Script>.Failure(scopeResult.Errors);

            scope = scopeResult.Value;
        }

        var now = DateTime.UtcNow;
        var channels = new List<Channel>
        {
            Channel.CreateDefault(ChannelKey.Production),
            Channel.CreateDefault(ChannelKey.Staging),
            Channel.CreateDefault(ChannelKey.Development),
        };

        return Result<Script>.Success(new Script(
            string.Empty,
            nameResult.Value!,
            scope,
            priority,
            description,
            channels,
            now,
            now));
    }

    public bool HasHostPatterns() => Scope?.Patterns.Count > 0;

    public bool MatchesHost(string host) => Scope?.Matches(host) ?? false;

    public Channel? FindChannel(ChannelKey key) =>
        _channels.FirstOrDefault(channel => channel.Key.Equals(key));

    public IResult AddCustomChannel(string? customName)
    {
        var keyResult = ChannelKey.Create(ChannelType.Custom, customName);
        if (keyResult.IsFailure)
            return Result.Failure(keyResult.Errors);

        if (FindChannel(keyResult.Value!) is not null)
            return Result.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ChannelAlreadyExists)
                .WithMessage("O canal customizado já existe para este script.")
                .Build());

        _channels.Add(Channel.CreateDefault(keyResult.Value!));
        Touch();
        return Result.Success();
    }

    public IResult Promote(ChannelKey channelKey, string releaseId)
    {
        releaseId = releaseId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(releaseId))
            return Result.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ReleaseIdInvalid)
                .WithMessage("O ID do release é obrigatório.")
                .Build());

        var channel = FindChannel(channelKey);
        if (channel is null)
            return Result.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ChannelNotFound)
                .WithMessage("Canal não encontrado para este script.")
                .Build());

        channel.Promote(releaseId);
        Touch();
        return Result.Success();
    }

    public IReadOnlyList<string> ClearReleaseReference(string releaseId)
    {
        releaseId = releaseId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(releaseId))
            return Array.Empty<string>();

        var cleared = new List<string>();

        foreach (var channel in _channels)
        {
            if (!string.Equals(channel.CurrentReleaseId, releaseId, StringComparison.Ordinal))
                continue;

            channel.ClearRelease();
            cleared.Add(channel.Key.ToRouteValue());
        }

        if (cleared.Count > 0)
            Touch();

        return cleared;
    }

    public IResult UpdateDescription(string? description)
    {
        description = description?.Trim();

        if (description?.Length > MaxDescriptionLength)
            return Result.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.DescriptionInvalid)
                .WithMessage($"A descrição não pode exceder {MaxDescriptionLength} caracteres.")
                .Build());

        Description = description;
        Touch();
        return Result.Success();
    }

    public IResult UpdateScope(IEnumerable<string>? hostPatterns)
    {
        var patterns = hostPatterns?.ToArray() ?? Array.Empty<string>();

        if (patterns.Length == 0)
        {
            Scope = null;
            Touch();
            return Result.Success();
        }

        var scopeResult = DeploymentScope.Create(patterns);
        if (scopeResult.IsFailure)
            return Result.Failure(scopeResult.Errors);

        Scope = scopeResult.Value;
        Touch();
        return Result.Success();
    }

    public IResult UpdatePriority(int priority)
    {
        Priority = priority;
        Touch();
        return Result.Success();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
