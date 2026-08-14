using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;
using Refactor.Nexus.Api.Operations.Domain.Errors;

namespace Refactor.Nexus.Api.Operations.Domain.Aggregates.Script;

public sealed class ScriptArtifact
{
    private ScriptArtifact(
        Guid id,
        OperationKey operationKey,
        string name,
        string body,
        bool enabled,
        DateTime createdAt,
        DateTime lastUpdatedAt)
    {
        Id = id;
        OperationKey = operationKey;
        Name = name;
        Body = body;
        Enabled = enabled;
        CreatedAt = createdAt;
        LastUpdatedAt = lastUpdatedAt;
    }

    public Guid Id { get; }
    public OperationKey OperationKey { get; }
    public string Name { get; private set; }
    public string Body { get; private set; }
    public bool Enabled { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime LastUpdatedAt { get; private set; }

    public static IResult<ScriptArtifact> Create(OperationKey operationKey, string name, string body)
    {
        if (string.IsNullOrWhiteSpace(operationKey.Value))
        {
            return Result<ScriptArtifact>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.KeyEmpty)
                .WithMessage("Operation key obrigatoria.")
                .Build());
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<ScriptArtifact>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.NameEmpty)
                .WithMessage("Nome do Script e obrigatorio.")
                .Build());
        }

        var now = DateTime.UtcNow;
        return Result<ScriptArtifact>.Success(new ScriptArtifact(
            Guid.NewGuid(),
            operationKey,
            name.Trim(),
            body ?? string.Empty,
            enabled: true,
            now,
            now));
    }

    public static ScriptArtifact Rehydrate(
        Guid id,
        OperationKey operationKey,
        string name,
        string body,
        bool enabled,
        DateTime createdAt,
        DateTime lastUpdatedAt) =>
        new(id, operationKey, name, body, enabled, createdAt, lastUpdatedAt);

    public void Update(string name, string body)
    {
        Name = name.Trim();
        Body = body ?? string.Empty;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
