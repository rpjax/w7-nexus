using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using MongoDB.Bson;
using Nexus.Accounts.Application.Services.Contracts;
using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Gateways.Wintech.Application.Services.Contracts;
using Nexus.Gateways.Wintech.Errors;

namespace Nexus.Gateways.Wintech.Application.Services;

public class WintechApiKeysService : IWintechApiKeysService
{
    private const int MaxNameLength = 200;
    private const int MaxKeyLength = 8192;
    private const int MaxStrawManIdLength = 256;

    private IWintechApiCredentialsRepository _credentials { get; }
    private IAccountRepository _accountRepository { get; }

    public WintechApiKeysService(
        IWintechApiCredentialsRepository credentials,
        IAccountRepository accountRepository)
    {
        _credentials = credentials;
        _accountRepository = accountRepository;
    }

    public Task<WintechApiCredentials?> GetRandomCredentialsAsync()
    {
        var enabled = _credentials.AsQueryable()
            .Where(c => c.Enabled)
            .ToList();

        if (enabled.Count == 0)
            return Task.FromResult<WintechApiCredentials?>(null);

        return Task.FromResult<WintechApiCredentials?>(enabled[Random.Shared.Next(enabled.Count)]);
    }

    public async Task<IResult<WintechApiCredentials>> AddCredentialsAsync(AddWintechCredentialsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = Result.Create<WintechApiCredentials>();

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length > MaxNameLength)
        {
            builder.WithError(Error.Create()
                .WithCode(WintechErrorCodes.NameTooLong)
                .WithMessage($"Name must be at most {MaxNameLength} characters.")
                .Build());
        }

        var publicKey = request.PublicKey?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(publicKey))
        {
            builder.WithError(Error.Create()
                .WithCode(WintechErrorCodes.PublicKeyRequired)
                .WithMessage("Public key is required.")
                .Build());
        }
        else if (publicKey.Length > MaxKeyLength)
        {
            builder.WithError(Error.Create()
                .WithCode(WintechErrorCodes.PublicKeyTooLong)
                .WithMessage($"Public key must be at most {MaxKeyLength} characters.")
                .Build());
        }

        var secretKey = request.SecretKey?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(secretKey))
        {
            builder.WithError(Error.Create()
                .WithCode(WintechErrorCodes.SecretKeyRequired)
                .WithMessage("Secret key is required.")
                .Build());
        }
        else if (secretKey.Length > MaxKeyLength)
        {
            builder.WithError(Error.Create()
                .WithCode(WintechErrorCodes.SecretKeyTooLong)
                .WithMessage($"Secret key must be at most {MaxKeyLength} characters.")
                .Build());
        }

        string? normalizedStrawMan = null;
        if (request.StrawManId is not null)
        {
            var sm = request.StrawManId.Trim();
            if (sm.Length == 0)
            {
                builder.WithError(Error.Create()
                    .WithCode(WintechErrorCodes.StrawManIdInvalid)
                    .WithMessage("Straw man id cannot be empty when provided.")
                    .Build());
            }
            else if (sm.Length > MaxStrawManIdLength)
            {
                builder.WithError(Error.Create()
                    .WithCode(WintechErrorCodes.StrawManIdTooLong)
                    .WithMessage($"Straw man id must be at most {MaxStrawManIdLength} characters.")
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
                .WithCode(WintechErrorCodes.PublicKeyAlreadyExists)
                .WithMessage("This public key is already registered.")
                .Build());
        }

        if (IsSecretKeyTakenByAnotherCredential(secretKey, excludeCredentialId: null))
        {
            builder.WithError(Error.Create()
                .WithCode(WintechErrorCodes.SecretKeyAlreadyExists)
                .WithMessage("This secret key is already registered.")
                .Build());
        }

        if (normalizedStrawMan is not null)
        {
            var accountExists = await _accountRepository.AsQueryable()
                .AnyAsync(a => a.Id == normalizedStrawMan);
            if (!accountExists)
            {
                builder.WithError(Error.Create()
                    .WithCode(WintechErrorCodes.StrawManAccountNotFound)
                    .WithMessage($"Straw man account '{normalizedStrawMan}' was not found.")
                    .Build());
            }
        }

        if (builder.ContainsError)
            return builder.Build();

        var credential = new WintechApiCredentials
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

    public async Task<IResult> UpdateCredentialsAsync(UpdateWintechCredentialsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = Result.Create();

        var idText = request.Id?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(idText) || !ObjectId.TryParse(idText, out _))
        {
            builder.WithError(Error.Create()
                .WithCode(WintechErrorCodes.CredentialIdInvalid)
                .WithMessage("A valid credential id is required.")
                .Build());
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length > MaxNameLength)
        {
            builder.WithError(Error.Create()
                .WithCode(WintechErrorCodes.NameTooLong)
                .WithMessage($"Name must be at most {MaxNameLength} characters.")
                .Build());
        }

        var publicKey = request.PublicKey?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(publicKey))
        {
            builder.WithError(Error.Create()
                .WithCode(WintechErrorCodes.PublicKeyRequired)
                .WithMessage("Public key is required.")
                .Build());
        }
        else if (publicKey.Length > MaxKeyLength)
        {
            builder.WithError(Error.Create()
                .WithCode(WintechErrorCodes.PublicKeyTooLong)
                .WithMessage($"Public key must be at most {MaxKeyLength} characters.")
                .Build());
        }

        var secretKey = request.SecretKey?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(secretKey))
        {
            builder.WithError(Error.Create()
                .WithCode(WintechErrorCodes.SecretKeyRequired)
                .WithMessage("Secret key is required.")
                .Build());
        }
        else if (secretKey.Length > MaxKeyLength)
        {
            builder.WithError(Error.Create()
                .WithCode(WintechErrorCodes.SecretKeyTooLong)
                .WithMessage($"Secret key must be at most {MaxKeyLength} characters.")
                .Build());
        }

        string? normalizedStrawMan = null;
        if (request.StrawManId is not null)
        {
            var sm = request.StrawManId.Trim();
            if (sm.Length == 0)
            {
                builder.WithError(Error.Create()
                    .WithCode(WintechErrorCodes.StrawManIdInvalid)
                    .WithMessage("Straw man id cannot be empty when provided.")
                    .Build());
            }
            else if (sm.Length > MaxStrawManIdLength)
            {
                builder.WithError(Error.Create()
                    .WithCode(WintechErrorCodes.StrawManIdTooLong)
                    .WithMessage($"Straw man id must be at most {MaxStrawManIdLength} characters.")
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
                .WithCode(WintechErrorCodes.PublicKeyAlreadyExists)
                .WithMessage("This public key is already registered for another credential.")
                .Build());
        }

        if (IsSecretKeyTakenByAnotherCredential(secretKey, excludeCredentialId: idText))
        {
            builder.WithError(Error.Create()
                .WithCode(WintechErrorCodes.SecretKeyAlreadyExists)
                .WithMessage("This secret key is already registered for another credential.")
                .Build());
        }

        if (normalizedStrawMan is not null)
        {
            var accountExists = await _accountRepository.AsQueryable()
                .AnyAsync(a => a.Id == normalizedStrawMan);
            if (!accountExists)
            {
                builder.WithError(Error.Create()
                    .WithCode(WintechErrorCodes.StrawManAccountNotFound)
                    .WithMessage($"Straw man account '{normalizedStrawMan}' was not found.")
                    .Build());
            }
        }

        if (builder.ContainsError)
            return builder.Build();

        var existing = _credentials.AsQueryable().FirstOrDefault(c => c.Id == idText);
        if (existing is null)
        {
            return Result.Failure(Error.Create()
                .WithCode(WintechErrorCodes.CredentialNotFound)
                .WithMessage($"Credential '{request.Id}' was not found.")
                .Build());
        }

        var updated = new WintechApiCredentials
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

    public async Task<IResult> SetCredentialEnabledAsync(SetWintechCredentialEnabledRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var idText = request.Id?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(idText) || !ObjectId.TryParse(idText, out _))
        {
            return Result.Failure(Error.Create()
                .WithCode(WintechErrorCodes.CredentialIdInvalid)
                .WithMessage("A valid credential id is required.")
                .Build());
        }

        var existing = _credentials.AsQueryable().FirstOrDefault(c => c.Id == idText);
        if (existing is null)
        {
            return Result.Failure(Error.Create()
                .WithCode(WintechErrorCodes.CredentialNotFound)
                .WithMessage($"Credential '{request.Id}' was not found.")
                .Build());
        }

        var updated = new WintechApiCredentials
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
                .WithCode(WintechErrorCodes.CredentialIdInvalid)
                .WithMessage("A valid credential id is required.")
                .Build());
        }

        var existing = _credentials.AsQueryable().FirstOrDefault(c => c.Id == id.Trim());
        if (existing is null)
        {
            return Result.Failure(Error.Create()
                .WithCode(WintechErrorCodes.CredentialNotFound)
                .WithMessage($"Credential '{id}' was not found.")
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
