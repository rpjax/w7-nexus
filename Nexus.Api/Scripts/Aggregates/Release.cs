using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Scripts.Errors;

namespace Nexus.Scripts.Aggregates;

public sealed class Release
{
    public string Id { get; }
    public string ScriptId { get; }
    public SemanticVersion Version { get; }
    public string SourceCode { get; }
    public ContentHash Hash { get; }
    public bool IsDeprecated { get; }
    public DateTime CreatedAt { get; }

    internal Release(
        string id,
        string scriptId,
        SemanticVersion version,
        string sourceCode,
        ContentHash hash,
        DateTime createdAt,
        bool isDeprecated = false)
    {
        Id = id;
        ScriptId = scriptId;
        Version = version;
        SourceCode = sourceCode;
        Hash = hash;
        IsDeprecated = isDeprecated;
        CreatedAt = createdAt;
    }

    public static IResult<Release> Publish(
        string scriptId,
        string sourceCode,
        SemanticVersion version)
    {
        scriptId = scriptId?.Trim() ?? string.Empty;
        sourceCode ??= string.Empty;

        if (string.IsNullOrWhiteSpace(scriptId))
            return Result<Release>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ScriptNotFound)
                .WithMessage("O ID do script é obrigatório.")
                .Build());

        if (string.IsNullOrWhiteSpace(sourceCode))
            return Result<Release>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.SourceCodeRequired)
                .WithMessage("O código-fonte é obrigatório.")
                .Build());

        if (version.Major < 0 || version.Minor < 0 || version.Patch < 0)
            return Result<Release>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.VersionInvalid)
                .WithMessage("A versão do release é inválida.")
                .Build());

        var hash = ContentHash.FromSourceCode(sourceCode);

        return Result<Release>.Success(new Release(
            string.Empty,
            scriptId,
            version,
            sourceCode,
            hash,
            DateTime.UtcNow));
    }

    public IResult<Release> Deprecate()
    {
        if (IsDeprecated)
            return Result<Release>.Success(this);

        return Result<Release>.Success(new Release(
            Id,
            ScriptId,
            Version,
            SourceCode,
            Hash,
            CreatedAt,
            isDeprecated: true));
    }

    public IResult<Release> Restore()
    {
        if (!IsDeprecated)
            return Result<Release>.Success(this);

        return Result<Release>.Success(new Release(
            Id,
            ScriptId,
            Version,
            SourceCode,
            Hash,
            CreatedAt,
            isDeprecated: false));
    }
}
