using System.Text.RegularExpressions;
using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Withdrawals.Errors;

namespace Nexus.Withdrawals.Aggregates;

public enum PixKeyType
{
    Cpf = 0,
    Cnpj,
    Email,
    Phone,
    Random,
}

public static partial class PixKeyRules
{
    public const int MaxEmailLength = 77;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^\+55[1-9]\d{9,10}$", RegexOptions.CultureInvariant)]
    private static partial Regex BrazilianPhonePattern();

    [GeneratedRegex(
        @"^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex RandomKeyPattern();

    public static IResult<(PixKeyType Type, string NormalizedKey)> ValidateAndNormalize(
        PixKeyType type,
        string? rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return Result<(PixKeyType, string)>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.PixKeyRequired)
                .WithMessage("A chave PIX é obrigatória.")
                .Build());
        }

        if (!Enum.IsDefined(type))
        {
            return Result<(PixKeyType, string)>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.PixKeyTypeInvalid)
                .WithMessage("O tipo da chave PIX é inválido.")
                .Build());
        }

        return type switch
        {
            PixKeyType.Cpf => NormalizeCpf(rawKey),
            PixKeyType.Cnpj => NormalizeCnpj(rawKey),
            PixKeyType.Email => NormalizeEmail(rawKey),
            PixKeyType.Phone => NormalizePhone(rawKey),
            PixKeyType.Random => NormalizeRandom(rawKey),
            _ => Result<(PixKeyType, string)>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.PixKeyTypeInvalid)
                .WithMessage("O tipo da chave PIX é inválido.")
                .Build()),
        };
    }

    private static IResult<(PixKeyType, string)> NormalizeCpf(string raw)
    {
        var digits = ExtractDigits(raw);
        if (digits.Length != 11 || !IsValidCpf(digits))
        {
            return InvalidKey(PixKeyType.Cpf);
        }

        return Result<(PixKeyType, string)>.Success((PixKeyType.Cpf, digits));
    }

    private static IResult<(PixKeyType, string)> NormalizeCnpj(string raw)
    {
        var digits = ExtractDigits(raw);
        if (digits.Length != 14 || !IsValidCnpj(digits))
        {
            return InvalidKey(PixKeyType.Cnpj);
        }

        return Result<(PixKeyType, string)>.Success((PixKeyType.Cnpj, digits));
    }

    private static IResult<(PixKeyType, string)> NormalizeEmail(string raw)
    {
        var normalized = raw.Trim().ToLowerInvariant();
        if (normalized.Length > MaxEmailLength || !EmailPattern().IsMatch(normalized))
        {
            return InvalidKey(PixKeyType.Email);
        }

        return Result<(PixKeyType, string)>.Success((PixKeyType.Email, normalized));
    }

    private static IResult<(PixKeyType, string)> NormalizePhone(string raw)
    {
        var trimmed = raw.Trim();
        string candidate;

        if (trimmed.StartsWith("+", StringComparison.Ordinal))
        {
            candidate = "+" + ExtractDigits(trimmed);
        }
        else
        {
            var digits = ExtractDigits(trimmed);
            if (digits.Length is >= 10 and <= 11)
            {
                candidate = "+55" + digits;
            }
            else if (digits.StartsWith("55", StringComparison.Ordinal) && digits.Length is 12 or 13)
            {
                candidate = "+" + digits;
            }
            else
            {
                return InvalidKey(PixKeyType.Phone);
            }
        }

        if (!BrazilianPhonePattern().IsMatch(candidate))
        {
            return InvalidKey(PixKeyType.Phone);
        }

        return Result<(PixKeyType, string)>.Success((PixKeyType.Phone, candidate));
    }

    private static IResult<(PixKeyType, string)> NormalizeRandom(string raw)
    {
        var trimmed = raw.Trim().ToLowerInvariant();
        if (Guid.TryParse(trimmed, out var guid))
        {
            trimmed = guid.ToString("D");
        }
        else if (ExtractHex(trimmed).Length == 32)
        {
            var hex = ExtractHex(trimmed);
            trimmed = $"{hex[..8]}-{hex[8..12]}-{hex[12..16]}-{hex[16..20]}-{hex[20..]}";
        }

        if (!RandomKeyPattern().IsMatch(trimmed))
        {
            return InvalidKey(PixKeyType.Random);
        }

        return Result<(PixKeyType, string)>.Success((PixKeyType.Random, trimmed));
    }

    private static IResult<(PixKeyType, string)> InvalidKey(PixKeyType type)
    {
        var message = type switch
        {
            PixKeyType.Cpf => "Informe um CPF válido com 11 dígitos.",
            PixKeyType.Cnpj => "Informe um CNPJ válido com 14 dígitos.",
            PixKeyType.Email => "Informe um e-mail válido (até 77 caracteres), conforme regras do PIX.",
            PixKeyType.Phone => "Informe um telefone válido no formato E.164 (+55DDDNNNNNNNNN).",
            PixKeyType.Random => "Informe uma chave aleatória (EVP) UUID válida.",
            _ => "A chave PIX é inválida para o tipo informado.",
        };

        return Result<(PixKeyType, string)>.Failure(Error.Create()
            .WithCode(BankAccountErrorCodes.PixKeyInvalid)
            .WithMessage(message)
            .Build());
    }

    private static string ExtractDigits(string value)
    {
        return string.Concat(value.Where(char.IsDigit));
    }

    private static string ExtractHex(string value)
    {
        return string.Concat(value.Where(static c => Uri.IsHexDigit(c))).ToLowerInvariant();
    }

    private static bool IsValidCpf(string digits)
    {
        if (AllSameDigit(digits))
        {
            return false;
        }

        Span<int> numbers = stackalloc int[11];
        for (var i = 0; i < 11; i++)
        {
            numbers[i] = digits[i] - '0';
        }

        var first = CalculateMod11Verifier(numbers[..9], 10);
        if (numbers[9] != first)
        {
            return false;
        }

        var second = CalculateMod11Verifier(numbers[..10], 11);
        return numbers[10] == second;
    }

    private static bool IsValidCnpj(string digits)
    {
        if (AllSameDigit(digits))
        {
            return false;
        }

        ReadOnlySpan<int> weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        ReadOnlySpan<int> weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var first = CalculateWeightedVerifier(digits.AsSpan(0, 12), weights1);
        if (digits[12] - '0' != first)
        {
            return false;
        }

        var second = CalculateWeightedVerifier(digits.AsSpan(0, 13), weights2);
        return digits[13] - '0' == second;
    }

    private static int CalculateMod11Verifier(ReadOnlySpan<int> source, int initialWeight)
    {
        var sum = 0;
        for (var i = 0; i < source.Length; i++)
        {
            sum += source[i] * (initialWeight - i);
        }

        var mod = sum % 11;
        return mod < 2 ? 0 : 11 - mod;
    }

    private static int CalculateWeightedVerifier(ReadOnlySpan<char> source, ReadOnlySpan<int> weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
        {
            sum += (source[i] - '0') * weights[i];
        }

        var mod = sum % 11;
        return mod < 2 ? 0 : 11 - mod;
    }

    private static bool AllSameDigit(string digits)
    {
        return digits.Distinct().Count() == 1;
    }
}
