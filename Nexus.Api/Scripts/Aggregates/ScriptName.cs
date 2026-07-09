using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Scripts.Errors;

namespace Nexus.Scripts.Aggregates;

public sealed class ScriptName : IEquatable<ScriptName>
{
    public const int MaxLength = 100;

    public string Value { get; }

    private ScriptName(string value) => Value = value;

    public static IResult<ScriptName> Create(string? value)
    {
        value = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return Result<ScriptName>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.NameInvalid)
                .WithMessage("O nome do script é obrigatório.")
                .Build());

        if (value.Length > MaxLength)
            return Result<ScriptName>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.NameInvalid)
                .WithMessage($"O nome do script não pode exceder {MaxLength} caracteres.")
                .Build());

        if (value.Any(char.IsWhiteSpace))
            return Result<ScriptName>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.NameInvalid)
                .WithMessage("O nome do script não pode conter espaços.")
                .Build());

        return Result<ScriptName>.Success(new ScriptName(value));
    }

    public bool Equals(ScriptName? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as ScriptName);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public override string ToString() => Value;
}
