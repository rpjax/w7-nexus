using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using MongoDB.Bson;
using Nexus.Accounts.Application.Services.Contracts;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Services.Contracts;
using Nexus.Gateways.SigiloPay.Errors;

namespace Nexus.Gateways.SigiloPay.Application.Services;

public class SigiloPayApiKeysService : ISigiloPayApiKeysService
{
    private const int MaxNameLength = 200;
    private const int MaxKeyLength = 8192;
    private const int MaxStrawManIdLength = 256;

    private ISigiloPayApiCredentialsRepository _credentials { get; }
    private IAccountRepository _accountRepository { get; }

    public SigiloPayApiKeysService(
        ISigiloPayApiCredentialsRepository credentials,
        IAccountRepository accountRepository)
    {
        _credentials = credentials;
        _accountRepository = accountRepository;
    }

    public Task<SigiloPayApiCredentials?> GetRandomCredentialsAsync()
    {
        var enabled = _credentials.AsQueryable()
            .Where(c => c.Enabled)
            .ToList();

        if (enabled.Count == 0)
            return Task.FromResult<SigiloPayApiCredentials?>(null);

        return Task.FromResult<SigiloPayApiCredentials?>(enabled[Random.Shared.Next(enabled.Count)]);
    }

    public async Task<IResult<SigiloPayApiCredentials>> AddCredentialsAsync(AddSigiloPayCredentialsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = Result.Create<SigiloPayApiCredentials>();

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length > MaxNameLength)
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.NameTooLong)
                .WithMessage($"O nome pode ter no máximo {MaxNameLength} caracteres.")
                .Build());
        }

        var publicKey = request.PublicKey?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(publicKey))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.PublicKeyRequired)
                .WithMessage("A chave pública é obrigatória.")
                .Build());
        }
        else if (publicKey.Length > MaxKeyLength)
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.PublicKeyTooLong)
                .WithMessage($"A chave pública pode ter no máximo {MaxKeyLength} caracteres.")
                .Build());
        }

        var secretKey = request.SecretKey?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(secretKey))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.SecretKeyRequired)
                .WithMessage("A chave secreta é obrigatória.")
                .Build());
        }
        else if (secretKey.Length > MaxKeyLength)
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.SecretKeyTooLong)
                .WithMessage($"A chave secreta pode ter no máximo {MaxKeyLength} caracteres.")
                .Build());
        }

        string? normalizedStrawMan = null;
        if (request.StrawManId is not null)
        {
            var sm = request.StrawManId.Trim();
            if (sm.Length == 0)
            {
                builder.WithError(Error.Create()
                    .WithCode(SigiloPayErrorCodes.StrawManIdInvalid)
                    .WithMessage("O ID do laranja não pode estar vazio quando informado.")
                    .Build());
            }
            else if (sm.Length > MaxStrawManIdLength)
            {
                builder.WithError(Error.Create()
                    .WithCode(SigiloPayErrorCodes.StrawManIdTooLong)
                    .WithMessage($"O ID do laranja pode ter no máximo {MaxStrawManIdLength} caracteres.")
                    .Build());
            }
            else
            {
                normalizedStrawMan = sm;
            }
        }

        if (builder.ContainsError)
            return builder.Build();

        if (IsPublicKeyTakenByAnotherCredential(publicKey, excludeCredentialId: null))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.PublicKeyAlreadyExists)
                .WithMessage("Esta chave pública já está cadastrada.")
                .Build());
        }

        if (IsSecretKeyTakenByAnotherCredential(secretKey, excludeCredentialId: null))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.SecretKeyAlreadyExists)
                .WithMessage("Esta chave secreta já está cadastrada.")
                .Build());
        }

        if (normalizedStrawMan is not null)
        {
            var accountExists = await _accountRepository.AsQueryable()
                .AnyAsync(a => a.Id == normalizedStrawMan);
            if (!accountExists)
            {
                builder.WithError(Error.Create()
                    .WithCode(SigiloPayErrorCodes.StrawManAccountNotFound)
                    .WithMessage($"A conta laranja '{normalizedStrawMan}' não foi encontrada.")
                    .Build());
            }
        }

        if (builder.ContainsError)
            return builder.Build();

        var credential = new SigiloPayApiCredentials
        {
            Id = string.Empty,
            StrawManId = normalizedStrawMan,
            Name = name,
            PublicKey = publicKey,
            SecretKey = secretKey,
            Enabled = request.Enabled
        };

        credential = await _credentials.CreateAsync(credential);
        return builder.WithValue(credential).Build();
    }

    public async Task<IResult> UpdateCredentialsAsync(UpdateSigiloPayCredentialsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = Result.Create();

        var idText = request.Id?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(idText) || !ObjectId.TryParse(idText, out _))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.CredentialIdInvalid)
                .WithMessage("Um ID de credencial válido é obrigatório.")
                .Build());
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length > MaxNameLength)
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.NameTooLong)
                .WithMessage($"O nome pode ter no máximo {MaxNameLength} caracteres.")
                .Build());
        }

        var publicKey = request.PublicKey?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(publicKey))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.PublicKeyRequired)
                .WithMessage("A chave pública é obrigatória.")
                .Build());
        }
        else if (publicKey.Length > MaxKeyLength)
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.PublicKeyTooLong)
                .WithMessage($"A chave pública pode ter no máximo {MaxKeyLength} caracteres.")
                .Build());
        }

        var secretKey = request.SecretKey?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(secretKey))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.SecretKeyRequired)
                .WithMessage("A chave secreta é obrigatória.")
                .Build());
        }
        else if (secretKey.Length > MaxKeyLength)
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.SecretKeyTooLong)
                .WithMessage($"A chave secreta pode ter no máximo {MaxKeyLength} caracteres.")
                .Build());
        }

        string? normalizedStrawMan = null;
        if (request.StrawManId is not null)
        {
            var sm = request.StrawManId.Trim();
            if (sm.Length == 0)
            {
                builder.WithError(Error.Create()
                    .WithCode(SigiloPayErrorCodes.StrawManIdInvalid)
                    .WithMessage("O ID do laranja não pode estar vazio quando informado.")
                    .Build());
            }
            else if (sm.Length > MaxStrawManIdLength)
            {
                builder.WithError(Error.Create()
                    .WithCode(SigiloPayErrorCodes.StrawManIdTooLong)
                    .WithMessage($"O ID do laranja pode ter no máximo {MaxStrawManIdLength} caracteres.")
                    .Build());
            }
            else
            {
                normalizedStrawMan = sm;
            }
        }

        if (builder.ContainsError)
            return builder.Build();

        if (IsPublicKeyTakenByAnotherCredential(publicKey, excludeCredentialId: idText))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.PublicKeyAlreadyExists)
                .WithMessage("Esta chave pública já está cadastrada em outra credencial.")
                .Build());
        }

        if (IsSecretKeyTakenByAnotherCredential(secretKey, excludeCredentialId: idText))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.SecretKeyAlreadyExists)
                .WithMessage("Esta chave secreta já está cadastrada em outra credencial.")
                .Build());
        }

        if (normalizedStrawMan is not null)
        {
            var accountExists = await _accountRepository.AsQueryable()
                .AnyAsync(a => a.Id == normalizedStrawMan);
            if (!accountExists)
            {
                builder.WithError(Error.Create()
                    .WithCode(SigiloPayErrorCodes.StrawManAccountNotFound)
                    .WithMessage($"A conta laranja '{normalizedStrawMan}' não foi encontrada.")
                    .Build());
            }
        }

        if (builder.ContainsError)
            return builder.Build();

        var existing = _credentials.AsQueryable().FirstOrDefault(c => c.Id == idText);
        if (existing is null)
        {
            return Result.Failure(Error.Create()
                .WithCode(SigiloPayErrorCodes.CredentialNotFound)
                .WithMessage($"A credencial '{request.Id}' não foi encontrada.")
                .Build());
        }

        var updated = new SigiloPayApiCredentials
        {
            Id = existing.Id,
            StrawManId = normalizedStrawMan,
            Name = name,
            PublicKey = publicKey,
            SecretKey = secretKey,
            Enabled = request.Enabled
        };

        await _credentials.UpdateAsync(updated);
        return Result.Success();
    }

    public async Task<IResult> SetCredentialEnabledAsync(SetSigiloPayCredentialEnabledRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var idText = request.Id?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(idText) || !ObjectId.TryParse(idText, out _))
        {
            return Result.Failure(Error.Create()
                .WithCode(SigiloPayErrorCodes.CredentialIdInvalid)
                .WithMessage("Um ID de credencial válido é obrigatório.")
                .Build());
        }

        var existing = _credentials.AsQueryable().FirstOrDefault(c => c.Id == idText);
        if (existing is null)
        {
            return Result.Failure(Error.Create()
                .WithCode(SigiloPayErrorCodes.CredentialNotFound)
                .WithMessage($"A credencial '{request.Id}' não foi encontrada.")
                .Build());
        }

        var updated = new SigiloPayApiCredentials
        {
            Id = existing.Id,
            StrawManId = existing.StrawManId,
            Name = existing.Name,
            PublicKey = existing.PublicKey,
            SecretKey = existing.SecretKey,
            Enabled = request.Enabled
        };

        await _credentials.UpdateAsync(updated);
        return Result.Success();
    }

    public async Task<IResult> DeleteCredentialsAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id.Trim(), out _))
        {
            return Result.Failure(Error.Create()
                .WithCode(SigiloPayErrorCodes.CredentialIdInvalid)
                .WithMessage("Um ID de credencial válido é obrigatório.")
                .Build());
        }

        var existing = _credentials.AsQueryable().FirstOrDefault(c => c.Id == id.Trim());
        if (existing is null)
        {
            return Result.Failure(Error.Create()
                .WithCode(SigiloPayErrorCodes.CredentialNotFound)
                .WithMessage($"A credencial '{id}' não foi encontrada.")
                .Build());
        }

        await _credentials.DeleteAsync(existing);
        return Result.Success();
    }

    private bool IsPublicKeyTakenByAnotherCredential(string publicKey, string? excludeCredentialId)
    {
        var matches = _credentials.AsQueryable()
            .Where(c => c.PublicKey == publicKey)
            .ToList();

        return excludeCredentialId is null
            ? matches.Count > 0
            : matches.Any(c => c.Id != excludeCredentialId);
    }

    private bool IsSecretKeyTakenByAnotherCredential(string secretKey, string? excludeCredentialId)
    {
        var matches = _credentials.AsQueryable()
            .Where(c => c.SecretKey == secretKey)
            .ToList();

        return excludeCredentialId is null
            ? matches.Count > 0
            : matches.Any(c => c.Id != excludeCredentialId);
    }
}
