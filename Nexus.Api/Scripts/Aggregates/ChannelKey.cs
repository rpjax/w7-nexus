using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Scripts.Errors;

namespace Nexus.Scripts.Aggregates;

public sealed class ChannelKey : IEquatable<ChannelKey>
{
    public ChannelType Type { get; }
    public string? CustomName { get; }

    private ChannelKey(ChannelType type, string? customName)
    {
        Type = type;
        CustomName = customName;
    }

    public static ChannelKey Production { get; } = new(ChannelType.Production, null);
    public static ChannelKey Staging { get; } = new(ChannelType.Staging, null);
    public static ChannelKey Development { get; } = new(ChannelType.Development, null);

    public static IResult<ChannelKey> Create(ChannelType type, string? customName)
    {
        customName = customName?.Trim();

        if (type == ChannelType.Custom)
        {
            if (string.IsNullOrWhiteSpace(customName))
                return Result<ChannelKey>.Failure(Error.Create()
                    .WithCode(ScriptErrorCodes.CustomChannelNameRequired)
                    .WithMessage("O nome do canal customizado é obrigatório.")
                    .Build());

            return Result<ChannelKey>.Success(new ChannelKey(type, customName));
        }

        if (!string.IsNullOrWhiteSpace(customName))
            return Result<ChannelKey>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ChannelKeyInvalid)
                .WithMessage("Canais padrão não aceitam nome customizado.")
                .Build());

        return type switch
        {
            ChannelType.Production => Result<ChannelKey>.Success(Production),
            ChannelType.Staging => Result<ChannelKey>.Success(Staging),
            ChannelType.Development => Result<ChannelKey>.Success(Development),
            _ => Result<ChannelKey>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ChannelKeyInvalid)
                .WithMessage("Tipo de canal inválido.")
                .Build()),
        };
    }

    public static IResult<ChannelKey> Parse(string? value)
    {
        value = value?.Trim();

        if (string.IsNullOrWhiteSpace(value) || value.Equals("prod", StringComparison.OrdinalIgnoreCase)
            || value.Equals("production", StringComparison.OrdinalIgnoreCase))
            return Result<ChannelKey>.Success(Production);

        if (value.Equals("staging", StringComparison.OrdinalIgnoreCase))
            return Result<ChannelKey>.Success(Staging);

        if (value.Equals("dev", StringComparison.OrdinalIgnoreCase)
            || value.Equals("development", StringComparison.OrdinalIgnoreCase))
            return Result<ChannelKey>.Success(Development);

        return Create(ChannelType.Custom, value);
    }

    public string ToRouteValue() =>
        Type switch
        {
            ChannelType.Production => "prod",
            ChannelType.Staging => "staging",
            ChannelType.Development => "development",
            ChannelType.Custom => CustomName!,
            _ => "prod",
        };

    public bool Equals(ChannelKey? other)
    {
        if (other is null)
            return false;

        if (Type != other.Type)
            return false;

        return Type == ChannelType.Custom
            ? string.Equals(CustomName, other.CustomName, StringComparison.OrdinalIgnoreCase)
            : true;
    }

    public override bool Equals(object? obj) => Equals(obj as ChannelKey);

    public override int GetHashCode() =>
        Type == ChannelType.Custom
            ? HashCode.Combine(Type, StringComparer.OrdinalIgnoreCase.GetHashCode(CustomName ?? string.Empty))
            : Type.GetHashCode();

    public override string ToString() => ToRouteValue();
}
