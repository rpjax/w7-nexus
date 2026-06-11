using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using MongoDB.Bson;
using Nexus.Accounts.Application.Services.Contracts;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.Frendz.Application.Services.Contracts;
using Nexus.Gateways.Frendz.Errors;

namespace Nexus.Gateways.Frendz.Application.Services;

public class FrendzApiKeysService : IFrendzApiKeysService
{
    private const int MaxNameLength = 200;
    private const int MaxTokenLength = 8192;
    private const int MaxStrawManIdLength = 256;

    private IFrendzApiCredentialsRepository _credentials { get; }
    private IAccountRepository _accountRepository { get; }

    public FrendzApiKeysService(
        IFrendzApiCredentialsRepository credentials,
        IAccountRepository accountRepository)
    {
        _credentials = credentials;
        _accountRepository = accountRepository;
    }

    public Task<FrendzApiCredentials?> GetRandomCredentialsAsync()
    {
        var enabled = _credentials.AsQueryable()
            .Where(c => c.Enabled)
            .ToList();

        if (enabled.Count == 0)
            return Task.FromResult<FrendzApiCredentials?>(null);

        return Task.FromResult<FrendzApiCredentials?>(enabled[Random.Shared.Next(enabled.Count)]);
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

        if (IsTokenTakenByAnotherCredential(token, excludeCredentialId: null))
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

        var credential = new FrendzApiCredentials
        {
            Id = string.Empty,
            StrawManId = normalizedStrawMan,
            Name = name,
            Token = token,
            Enabled = request.Enabled
        };

        credential = await _credentials.CreateAsync(credential);
        return builder.WithValue(credential).Build();
    }

    public async Task<IResult> UpdateCredentialsAsync(UpdateCredentialsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = Result.Create();

        var idText = request.Id?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(idText) || !ObjectId.TryParse(idText, out _))
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

        if (IsTokenTakenByAnotherCredential(token, excludeCredentialId: idText))
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

        var existing = _credentials.AsQueryable().FirstOrDefault(c => c.Id == idText);
        if (existing is null)
        {
            return Result.Failure(Error.Create()
                .WithCode(FrendzErrorCodes.CredentialNotFound)
                .WithMessage($"Credential '{request.Id}' was not found.")
                .Build());
        }

        var updated = new FrendzApiCredentials
        {
            Id = existing.Id,
            StrawManId = normalizedStrawMan,
            Name = name,
            Token = token,
            Enabled = request.Enabled
        };

        await _credentials.UpdateAsync(updated);
        return Result.Success();
    }

    public async Task<IResult> SetCredentialEnabledAsync(SetFrendzCredentialEnabledRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var idText = request.Id?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(idText) || !ObjectId.TryParse(idText, out _))
        {
            return Result.Failure(Error.Create()
                .WithCode(FrendzErrorCodes.CredentialIdInvalid)
                .WithMessage("A valid credential id is required.")
                .Build());
        }

        var existing = _credentials.AsQueryable().FirstOrDefault(c => c.Id == idText);
        if (existing is null)
        {
            return Result.Failure(Error.Create()
                .WithCode(FrendzErrorCodes.CredentialNotFound)
                .WithMessage($"Credential '{request.Id}' was not found.")
                .Build());
        }

        var updated = new FrendzApiCredentials
        {
            Id = existing.Id,
            StrawManId = existing.StrawManId,
            Name = existing.Name,
            Token = existing.Token,
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
                .WithCode(FrendzErrorCodes.CredentialIdInvalid)
                .WithMessage("A valid credential id is required.")
                .Build());
        }

        var existing = _credentials.AsQueryable().FirstOrDefault(c => c.Id == id.Trim());
        if (existing is null)
        {
            return Result.Failure(Error.Create()
                .WithCode(FrendzErrorCodes.CredentialNotFound)
                .WithMessage($"Credential '{id}' was not found.")
                .Build());
        }

        await _credentials.DeleteAsync(existing);
        return Result.Success();
    }

    private bool IsTokenTakenByAnotherCredential(string token, string? excludeCredentialId)
    {
        var matches = _credentials.AsQueryable()
            .Where(c => c.Token == token)
            .ToList();

        return excludeCredentialId is null
            ? matches.Count > 0
            : matches.Any(c => c.Id != excludeCredentialId);
    }
}
