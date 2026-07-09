using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Scripts.Errors;

namespace Nexus.Scripts.Aggregates;

public sealed class HostPattern : IEquatable<HostPattern>
{
    public string Value { get; }

    private HostPattern(string value) => Value = value;

    public static IResult<HostPattern> Create(string? value)
    {
        value = NormalizeInput(value);

        if (string.IsNullOrWhiteSpace(value))
            return Result<HostPattern>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.HostPatternInvalid)
                .WithMessage("O padrão de host é obrigatório.")
                .Build());

        if (value.Contains("://", StringComparison.Ordinal) || value.Contains('/', StringComparison.Ordinal))
            return Result<HostPattern>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.HostPatternInvalid)
                .WithMessage("O padrão de host não pode conter protocolo ou caminho.")
                .Build());

        if (value.Contains(':', StringComparison.Ordinal))
            return Result<HostPattern>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.HostPatternInvalid)
                .WithMessage("O padrão de host não pode conter porta.")
                .Build());

        if (value == "*")
            return Result<HostPattern>.Success(new HostPattern(value));

        if (value.StartsWith("*", StringComparison.Ordinal) && value != "*")
        {
            if (!value.StartsWith("*.", StringComparison.Ordinal))
            {
                return Result<HostPattern>.Failure(Error.Create()
                    .WithCode(ScriptErrorCodes.HostPatternInvalid)
                    .WithMessage("Wildcards só são permitidos como '*.domínio'.")
                    .Build());
            }

            var domain = value[2..];
            if (string.IsNullOrWhiteSpace(domain) || !domain.Contains('.', StringComparison.Ordinal))
            {
                return Result<HostPattern>.Failure(Error.Create()
                    .WithCode(ScriptErrorCodes.HostPatternInvalid)
                    .WithMessage("O domínio após o wildcard deve incluir um domínio base válido.")
                    .Build());
            }
        }

        if (value.Contains('*', StringComparison.Ordinal) && !value.StartsWith("*", StringComparison.Ordinal))
            return Result<HostPattern>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.HostPatternInvalid)
                .WithMessage("Wildcards só são permitidos no subdomínio.")
                .Build());

        return Result<HostPattern>.Success(new HostPattern(value));
    }

    public bool Matches(string requestHost)
    {
        var host = NormalizeInput(requestHost);

        if (string.IsNullOrWhiteSpace(host))
            return false;

        if (Value == "*")
            return true;

        if (Value.StartsWith("*.", StringComparison.Ordinal))
        {
            var suffix = Value[1..];
            return host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(host, suffix[1..], StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(host, Value, StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeHost(string? value)
    {
        var host = NormalizeInput(value);

        if (string.IsNullOrWhiteSpace(host))
            return string.Empty;

        if (Uri.TryCreate(host, UriKind.Absolute, out var uri))
            return uri.Host.ToLowerInvariant();

        var withoutPort = host.Split(':', 2)[0];
        return withoutPort.ToLowerInvariant();
    }

    private static string NormalizeInput(string? value) =>
        value?.Trim().ToLowerInvariant() ?? string.Empty;

    public bool Equals(HostPattern? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as HostPattern);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public override string ToString() => Value;
}
