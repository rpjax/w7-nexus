using Aidan.Core.Errors;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Aidan.Core.Patterns;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Accounts.Application.Contracts;
using Nexus.Database.Models;
using Nexus.Gateways.SigiloPay.Application;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.SigiloPay.ErrorCodes;

namespace Nexus.Gateways.SigiloPay.Application;

public class SigiloPayApiKeysService : ISigiloPayApiKeysService
{
    private const int MaxNameLength = 200;
    private const int MaxKeyLength = 8192;
    private const int MaxStrawManIdLength = 256;

    private IMongoCollection<SigiloPayApiCredentialsRecord> _credentialsCollection { get; }
    private IAccountRepository _accountRepository { get; }

    public SigiloPayApiKeysService(
        IMongoCollection<SigiloPayApiCredentialsRecord> credentialsCollection,
        IAccountRepository accountRepository)
    {
        _credentialsCollection = credentialsCollection;
        _accountRepository = accountRepository;
    }

    public async Task<SigiloPayApiCredentials?> GetRandomCredentialsAsync()
    {
        var filter = Builders<SigiloPayApiCredentialsRecord>.Filter.Eq(r => r.Enabled, true);
        var count = await _credentialsCollection.CountDocumentsAsync(filter);
        if (count == 0)
            return null;

        var skip = Random.Shared.Next(0, (int)count);
        var record = await _credentialsCollection
            .Find(filter)
            .Skip(skip)
            .Limit(1)
            .FirstOrDefaultAsync();

        return record is null ? null : ToModel(record);
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
                .WithMessage($"Name must be at most {MaxNameLength} characters.")
                .Build());
        }

        var publicKey = request.PublicKey?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(publicKey))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.PublicKeyRequired)
                .WithMessage("Public key is required.")
                .Build());
        }
        else if (publicKey.Length > MaxKeyLength)
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.PublicKeyTooLong)
                .WithMessage($"Public key must be at most {MaxKeyLength} characters.")
                .Build());
        }

        var secretKey = request.SecretKey?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(secretKey))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.SecretKeyRequired)
                .WithMessage("Secret key is required.")
                .Build());
        }
        else if (secretKey.Length > MaxKeyLength)
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.SecretKeyTooLong)
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
                    .WithCode(SigiloPayErrorCodes.StrawManIdInvalid)
                    .WithMessage("Straw man id cannot be empty when provided.")
                    .Build());
            }
            else if (sm.Length > MaxStrawManIdLength)
            {
                builder.WithError(Error.Create()
                    .WithCode(SigiloPayErrorCodes.StrawManIdTooLong)
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

        if (await IsPublicKeyTakenByAnotherCredentialAsync(publicKey, excludeCredentialId: null))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.PublicKeyAlreadyExists)
                .WithMessage("This public key is already registered.")
                .Build());
        }

        if (await IsSecretKeyTakenByAnotherCredentialAsync(secretKey, excludeCredentialId: null))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.SecretKeyAlreadyExists)
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
                    .WithCode(SigiloPayErrorCodes.StrawManAccountNotFound)
                    .WithMessage($"Straw man account '{normalizedStrawMan}' was not found.")
                    .Build());
            }
        }

        if (builder.ContainsError)
            return builder.Build();

        var record = new SigiloPayApiCredentialsRecord
        {
            Id = ObjectId.GenerateNewId(),
            StrawManId = normalizedStrawMan,
            Name = name,
            PublicKey = publicKey,
            SecretKey = secretKey,
            Enabled = request.Enabled
        };

        await _credentialsCollection.InsertOneAsync(record);
        return builder.WithValue(ToModel(record)).Build();
    }

    public async Task<IResult> UpdateCredentialsAsync(UpdateSigiloPayCredentialsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = Result.Create();

        var idText = request.Id?.Trim() ?? string.Empty;
        ObjectId objectId = default;
        if (string.IsNullOrWhiteSpace(idText))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.CredentialIdInvalid)
                .WithMessage("A valid credential id is required.")
                .Build());
        }
        else if (!ObjectId.TryParse(idText, out objectId))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.CredentialIdInvalid)
                .WithMessage("A valid credential id is required.")
                .Build());
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length > MaxNameLength)
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.NameTooLong)
                .WithMessage($"Name must be at most {MaxNameLength} characters.")
                .Build());
        }

        var publicKey = request.PublicKey?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(publicKey))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.PublicKeyRequired)
                .WithMessage("Public key is required.")
                .Build());
        }
        else if (publicKey.Length > MaxKeyLength)
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.PublicKeyTooLong)
                .WithMessage($"Public key must be at most {MaxKeyLength} characters.")
                .Build());
        }

        var secretKey = request.SecretKey?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(secretKey))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.SecretKeyRequired)
                .WithMessage("Secret key is required.")
                .Build());
        }
        else if (secretKey.Length > MaxKeyLength)
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.SecretKeyTooLong)
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
                    .WithCode(SigiloPayErrorCodes.StrawManIdInvalid)
                    .WithMessage("Straw man id cannot be empty when provided.")
                    .Build());
            }
            else if (sm.Length > MaxStrawManIdLength)
            {
                builder.WithError(Error.Create()
                    .WithCode(SigiloPayErrorCodes.StrawManIdTooLong)
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

        if (await IsPublicKeyTakenByAnotherCredentialAsync(publicKey, excludeCredentialId: objectId))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.PublicKeyAlreadyExists)
                .WithMessage("This public key is already registered for another credential.")
                .Build());
        }

        if (await IsSecretKeyTakenByAnotherCredentialAsync(secretKey, excludeCredentialId: objectId))
        {
            builder.WithError(Error.Create()
                .WithCode(SigiloPayErrorCodes.SecretKeyAlreadyExists)
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
                    .WithCode(SigiloPayErrorCodes.StrawManAccountNotFound)
                    .WithMessage($"Straw man account '{normalizedStrawMan}' was not found.")
                    .Build());
            }
        }

        if (builder.ContainsError)
            return builder.Build();

        var filter = Builders<SigiloPayApiCredentialsRecord>.Filter.Eq(r => r.Id, objectId);
        var update = Builders<SigiloPayApiCredentialsRecord>.Update
            .Set(r => r.StrawManId, normalizedStrawMan)
            .Set(r => r.PublicKey, publicKey)
            .Set(r => r.SecretKey, secretKey)
            .Set(r => r.Name, name)
            .Set(r => r.Enabled, request.Enabled);

        var result = await _credentialsCollection.UpdateOneAsync(filter, update);
        if (result.MatchedCount == 0)
        {
            return Result.Failure(Error.Create()
                .WithCode(SigiloPayErrorCodes.CredentialNotFound)
                .WithMessage($"Credential '{request.Id}' was not found.")
                .Build());
        }

        return Result.Success();
    }

    public async Task<IResult> SetCredentialEnabledAsync(SetSigiloPayCredentialEnabledRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var idText = request.Id?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(idText) || !ObjectId.TryParse(idText, out var objectId))
        {
            return Result.Failure(Error.Create()
                .WithCode(SigiloPayErrorCodes.CredentialIdInvalid)
                .WithMessage("A valid credential id is required.")
                .Build());
        }

        var filter = Builders<SigiloPayApiCredentialsRecord>.Filter.Eq(r => r.Id, objectId);
        var update = Builders<SigiloPayApiCredentialsRecord>.Update.Set(r => r.Enabled, request.Enabled);
        var result = await _credentialsCollection.UpdateOneAsync(filter, update);
        if (result.MatchedCount == 0)
        {
            return Result.Failure(Error.Create()
                .WithCode(SigiloPayErrorCodes.CredentialNotFound)
                .WithMessage($"Credential '{request.Id}' was not found.")
                .Build());
        }

        return Result.Success();
    }

    public async Task<IResult> DeleteCredentialsAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id.Trim(), out var objectId))
        {
            return Result.Failure(Error.Create()
                .WithCode(SigiloPayErrorCodes.CredentialIdInvalid)
                .WithMessage("A valid credential id is required.")
                .Build());
        }

        var filter = Builders<SigiloPayApiCredentialsRecord>.Filter.Eq(r => r.Id, objectId);
        var result = await _credentialsCollection.DeleteOneAsync(filter);
        if (result.DeletedCount == 0)
        {
            return Result.Failure(Error.Create()
                .WithCode(SigiloPayErrorCodes.CredentialNotFound)
                .WithMessage($"Credential '{id}' was not found.")
                .Build());
        }

        return Result.Success();
    }

    private async Task<bool> IsPublicKeyTakenByAnotherCredentialAsync(string publicKey, ObjectId? excludeCredentialId)
    {
        var filter = Builders<SigiloPayApiCredentialsRecord>.Filter.Eq(r => r.PublicKey, publicKey);
        if (excludeCredentialId is not null)
        {
            filter = Builders<SigiloPayApiCredentialsRecord>.Filter.And(
                filter,
                Builders<SigiloPayApiCredentialsRecord>.Filter.Ne(r => r.Id, excludeCredentialId.Value));
        }

        var count = await _credentialsCollection.CountDocumentsAsync(filter);
        return count > 0;
    }

    private async Task<bool> IsSecretKeyTakenByAnotherCredentialAsync(string secretKey, ObjectId? excludeCredentialId)
    {
        var filter = Builders<SigiloPayApiCredentialsRecord>.Filter.Eq(r => r.SecretKey, secretKey);
        if (excludeCredentialId is not null)
        {
            filter = Builders<SigiloPayApiCredentialsRecord>.Filter.And(
                filter,
                Builders<SigiloPayApiCredentialsRecord>.Filter.Ne(r => r.Id, excludeCredentialId.Value));
        }

        var count = await _credentialsCollection.CountDocumentsAsync(filter);
        return count > 0;
    }

    private static SigiloPayApiCredentials ToModel(SigiloPayApiCredentialsRecord record) =>
        new()
        {
            Id = record.Id.ToString(),
            StrawManId = record.StrawManId,
            Name = record.Name,
            PublicKey = record.PublicKey,
            SecretKey = record.SecretKey,
            Enabled = record.Enabled
        };
}
