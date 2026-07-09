using System.Security.Cryptography;
using System.Text;
using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Scripts.Errors;

namespace Nexus.Scripts.Aggregates;

public sealed class ContentHash : IEquatable<ContentHash>
{
    public string Value { get; }

    private ContentHash(string value) => Value = value;

    public static ContentHash FromSourceCode(string sourceCode)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sourceCode));
        return new ContentHash(Convert.ToHexString(bytes).ToLowerInvariant());
    }

    public static IResult<ContentHash> Create(string? value)
    {
        value = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return Result<ContentHash>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.SourceCodeRequired)
                .WithMessage("O hash do conteúdo é obrigatório.")
                .Build());

        return Result<ContentHash>.Success(new ContentHash(value));
    }

    public bool Equals(ContentHash? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as ContentHash);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}
