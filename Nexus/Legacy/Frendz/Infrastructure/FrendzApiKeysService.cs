using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Accounts.Application;
using Nexus.Legacy.Database.Models;
using Nexus.Legacy.Frendz.Application;
using Nexus.Legacy.Frendz.Application.Models;
using Nexus.Legacy.Frendz.ErrorCodes;

namespace Nexus.Legacy.Frendz.Infrastructure;

public class FrendzApiKeysService : IFrendzApiKeysService
{
    private const int MaxNameLength = 200;
    private const int MaxTokenLength = 8192;
    private const int MaxStrawManIdLength = 256;

    private IMongoCollection<FrendzApiCredentialsRecord> _credentialsCollection { get; }
    private IAccountRepository _accountRepository { get; }

    public FrendzApiKeysService(
        IMongoCollection<FrendzApiCredentialsRecord> credentialsCollection,
        IAccountRepository accountRepository)
    {
        _credentialsCollection = credentialsCollection;
        _accountRepository = accountRepository;
    }

    public async Task<FrendzApiCredentials?> GetRandomCredentialsAsync()
    {
        var filter = Builders<FrendzApiCredentialsRecord>.Filter.Eq(r => r.Enabled, true);
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

    public async Task<IResult<FrendzApiCredentials>> AddCredentialsAsync(AddCredentialsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = Result.Create<FrendzApiCredentials>();

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length > MaxNameLength)
        {
            builder.WithError(Error.Create()
                .WithCode(FrendzErrorCodes.NameTooLong)
                .WithMessage($"Name must be at most {MaxNameLength} characters.")
                .Build());
        }

        var token = request.Token?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(token))
        {
            builder.WithError(Error.Create()
                .WithCode(FrendzErrorCodes.TokenRequired)
                .WithMessage("Token is required.")
                .Build());
        }
        else if (token.Length > MaxTokenLength)
        {
            builder.WithError(Error.Create()
                .WithCode(FrendzErrorCodes.TokenTooLong)
                .WithMessage($"Token must be at most {MaxTokenLength} characters.")
                .Build());
        }

        string? normalizedStrawMan = null;
        if (request.StrawManId is not null)
        {
            var sm = request.StrawManId.Trim();
            if (sm.Length == 0)
            {
                builder.WithError(Error.Create()
                    .WithCode(FrendzErrorCodes.StrawManIdInvalid)
                    .WithMessage("Straw man id cannot be empty when provided.")
                    .Build());
            }
            else if (sm.Length > MaxStrawManIdLength)
            {
                builder.WithError(Error.Create()
                    .WithCode(FrendzErrorCodes.StrawManIdTooLong)
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

        if (await IsTokenTakenByAnotherCredentialAsync(token, excludeCredentialId: null))
        {
            builder.WithError(Error.Create()
                .WithCode(FrendzErrorCodes.TokenAlreadyExists)
                .WithMessage("This token is already registered.")
                .Build());
        }

        if (normalizedStrawMan is not null)
        {
            var accountExists = await _accountRepository.AsQueryable()
                .AnyAsync(a => a.Id == normalizedStrawMan);
            if (!accountExists)
            {
                builder.WithError(Error.Create()
                    .WithCode(FrendzErrorCodes.StrawManAccountNotFound)
                    .WithMessage($"Straw man account '{normalizedStrawMan}' was not found.")
                    .Build());
            }
        }

        if (builder.ContainsError)
            return builder.Build();

        var record = new FrendzApiCredentialsRecord
        {
            Id = ObjectId.GenerateNewId(),
            StrawManId = normalizedStrawMan,
            Name = name,
            Token = token,
            Enabled = request.Enabled
        };

        await _credentialsCollection.InsertOneAsync(record);
        return builder.WithValue(ToModel(record)).Build();
    }

    public async Task<IResult> UpdateCredentialsAsync(UpdateCredentialsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = Result.Create();

        var idText = request.Id?.Trim() ?? string.Empty;
        ObjectId objectId = default;
        if (string.IsNullOrWhiteSpace(idText))
        {
            builder.WithError(Error.Create()
                .WithCode(FrendzErrorCodes.CredentialIdInvalid)
                .WithMessage("A valid credential id is required.")
                .Build());
        }
        else if (!ObjectId.TryParse(idText, out objectId))
        {
            builder.WithError(Error.Create()
                .WithCode(FrendzErrorCodes.CredentialIdInvalid)
                .WithMessage("A valid credential id is required.")
                .Build());
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length > MaxNameLength)
        {
            builder.WithError(Error.Create()
                .WithCode(FrendzErrorCodes.NameTooLong)
                .WithMessage($"Name must be at most {MaxNameLength} characters.")
                .Build());
        }

        var token = request.Token?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(token))
        {
            builder.WithError(Error.Create()
                .WithCode(FrendzErrorCodes.TokenRequired)
                .WithMessage("Token is required.")
                .Build());
        }
        else if (token.Length > MaxTokenLength)
        {
            builder.WithError(Error.Create()
                .WithCode(FrendzErrorCodes.TokenTooLong)
                .WithMessage($"Token must be at most {MaxTokenLength} characters.")
                .Build());
        }

        string? normalizedStrawMan = null;
        if (request.StrawManId is not null)
        {
            var sm = request.StrawManId.Trim();
            if (sm.Length == 0)
            {
                builder.WithError(Error.Create()
                    .WithCode(FrendzErrorCodes.StrawManIdInvalid)
                    .WithMessage("Straw man id cannot be empty when provided.")
                    .Build());
            }
            else if (sm.Length > MaxStrawManIdLength)
            {
                builder.WithError(Error.Create()
                    .WithCode(FrendzErrorCodes.StrawManIdTooLong)
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

        if (await IsTokenTakenByAnotherCredentialAsync(token, excludeCredentialId: objectId))
        {
            builder.WithError(Error.Create()
                .WithCode(FrendzErrorCodes.TokenAlreadyExists)
                .WithMessage("This token is already registered for another credential.")
                .Build());
        }

        if (normalizedStrawMan is not null)
        {
            var accountExists = await _accountRepository.AsQueryable()
                .AnyAsync(a => a.Id == normalizedStrawMan);
            if (!accountExists)
            {
                builder.WithError(Error.Create()
                    .WithCode(FrendzErrorCodes.StrawManAccountNotFound)
                    .WithMessage($"Straw man account '{normalizedStrawMan}' was not found.")
                    .Build());
            }
        }

        if (builder.ContainsError)
            return builder.Build();

        var filter = Builders<FrendzApiCredentialsRecord>.Filter.Eq(r => r.Id, objectId);
        var update = Builders<FrendzApiCredentialsRecord>.Update
            .Set(r => r.StrawManId, normalizedStrawMan)
            .Set(r => r.Token, token)
            .Set(r => r.Name, name)
            .Set(r => r.Enabled, request.Enabled);

        var result = await _credentialsCollection.UpdateOneAsync(filter, update);
        if (result.MatchedCount == 0)
        {
            return Result.Failure(Error.Create()
                .WithCode(FrendzErrorCodes.CredentialNotFound)
                .WithMessage($"Credential '{request.Id}' was not found.")
                .Build());
        }

        return Result.Success();
    }

    public async Task<IResult> SetCredentialEnabledAsync(SetFrendzCredentialEnabledRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var idText = request.Id?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(idText) || !ObjectId.TryParse(idText, out var objectId))
        {
            return Result.Failure(Error.Create()
                .WithCode(FrendzErrorCodes.CredentialIdInvalid)
                .WithMessage("A valid credential id is required.")
                .Build());
        }

        var filter = Builders<FrendzApiCredentialsRecord>.Filter.Eq(r => r.Id, objectId);
        var update = Builders<FrendzApiCredentialsRecord>.Update.Set(r => r.Enabled, request.Enabled);
        var result = await _credentialsCollection.UpdateOneAsync(filter, update);
        if (result.MatchedCount == 0)
        {
            return Result.Failure(Error.Create()
                .WithCode(FrendzErrorCodes.CredentialNotFound)
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
                .WithCode(FrendzErrorCodes.CredentialIdInvalid)
                .WithMessage("A valid credential id is required.")
                .Build());
        }

        var filter = Builders<FrendzApiCredentialsRecord>.Filter.Eq(r => r.Id, objectId);
        var result = await _credentialsCollection.DeleteOneAsync(filter);
        if (result.DeletedCount == 0)
        {
            return Result.Failure(Error.Create()
                .WithCode(FrendzErrorCodes.CredentialNotFound)
                .WithMessage($"Credential '{id}' was not found.")
                .Build());
        }

        return Result.Success();
    }

    private async Task<bool> IsTokenTakenByAnotherCredentialAsync(string token, ObjectId? excludeCredentialId)
    {
        var filter = Builders<FrendzApiCredentialsRecord>.Filter.Eq(r => r.Token, token);
        if (excludeCredentialId is not null)
        {
            filter = Builders<FrendzApiCredentialsRecord>.Filter.And(
                filter,
                Builders<FrendzApiCredentialsRecord>.Filter.Ne(r => r.Id, excludeCredentialId.Value));
        }

        var count = await _credentialsCollection.CountDocumentsAsync(filter);
        return count > 0;
    }

    private static FrendzApiCredentials ToModel(FrendzApiCredentialsRecord record) =>
        new()
        {
            Id = record.Id.ToString(),
            StrawManId = record.StrawManId,
            Name = record.Name,
            Token = record.Token,
            Enabled = record.Enabled
        };
}
