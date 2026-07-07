using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Application.Requests.Administrator;
using Nexus.Gateways.Application.Responses.Administrator;
using Nexus.Gateways.Errors;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.Wintech.Application.Contracts;

namespace Nexus.Gateways.Application.Services;

public sealed class AdministratorGatewayCredentialsSearchService : IAdministratorGatewayCredentialsSearchService
{
    private const int SearchKeywordMaxLength = 200;

    private IFrendzApiCredentialsRepository _frendzCredentials { get; }
    private IWintechApiCredentialsRepository _wintechCredentials { get; }
    private ISigiloPayApiCredentialsRepository _sigiloPayCredentials { get; }

    public AdministratorGatewayCredentialsSearchService(
        IFrendzApiCredentialsRepository frendzCredentials,
        IWintechApiCredentialsRepository wintechCredentials,
        ISigiloPayApiCredentialsRepository sigiloPayCredentials)
    {
        _frendzCredentials = frendzCredentials;
        _wintechCredentials = wintechCredentials;
        _sigiloPayCredentials = sigiloPayCredentials;
    }

    public async Task<IResult<SearchGatewayCredentialsResponse>> SearchCredentialsAsync(
        PaymentGateway provider,
        SearchGatewayCredentialsRequest? request)
    {
        return provider switch
        {
            PaymentGateway.Frendz => await SearchFrendzAsync(request),
            PaymentGateway.Wintech => await SearchWintechAsync(request),
            PaymentGateway.SigiloPay => await SearchSigiloPayAsync(request),
            _ => Result<SearchGatewayCredentialsResponse>.Failure(Error.Create()
                .WithCode(GatewayAdministratorErrorCodes.ProviderInvalid)
                .WithMessage("O provedor do gateway não é suportado para busca de credenciais.")
                .Build()),
        };
    }

    private async Task<IResult<SearchGatewayCredentialsResponse>> SearchFrendzAsync(
        SearchGatewayCredentialsRequest? request)
    {
        var validation = ValidateSearchRequest(request);
        if (validation.IsFailure)
            return Result<SearchGatewayCredentialsResponse>.Failure(validation.Errors);

        var (limit, offset, keyword, enabledOnly) = validation.Value;
        var query = _frendzCredentials.AsQueryable();

        if (enabledOnly == true)
            query = query.Where(c => c.Enabled);
        else if (enabledOnly == false)
            query = query.Where(c => !c.Enabled);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.ToLowerInvariant();
            query = query.Where(c =>
                c.Id.ToLower().Contains(term)
                || c.Name.ToLower().Contains(term)
                || c.Token.ToLower().Contains(term)
                || (c.StrawManId != null && c.StrawManId.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync();

        return Result<SearchGatewayCredentialsResponse>.Success(new SearchGatewayCredentialsResponse
        {
            Total = total,
            Items = items,
        });
    }

    private async Task<IResult<SearchGatewayCredentialsResponse>> SearchWintechAsync(
        SearchGatewayCredentialsRequest? request)
    {
        var validation = ValidateSearchRequest(request);
        if (validation.IsFailure)
            return Result<SearchGatewayCredentialsResponse>.Failure(validation.Errors);

        var (limit, offset, keyword, enabledOnly) = validation.Value;
        var query = _wintechCredentials.AsQueryable();

        if (enabledOnly == true)
            query = query.Where(c => c.Enabled);
        else if (enabledOnly == false)
            query = query.Where(c => !c.Enabled);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.ToLowerInvariant();
            query = query.Where(c =>
                c.Id.ToLower().Contains(term)
                || c.Name.ToLower().Contains(term)
                || c.PublicKey.ToLower().Contains(term)
                || c.SecretKey.ToLower().Contains(term)
                || (c.StrawManId != null && c.StrawManId.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync();

        return Result<SearchGatewayCredentialsResponse>.Success(new SearchGatewayCredentialsResponse
        {
            Total = total,
            Items = items,
        });
    }

    private async Task<IResult<SearchGatewayCredentialsResponse>> SearchSigiloPayAsync(
        SearchGatewayCredentialsRequest? request)
    {
        var validation = ValidateSearchRequest(request);
        if (validation.IsFailure)
            return Result<SearchGatewayCredentialsResponse>.Failure(validation.Errors);

        var (limit, offset, keyword, enabledOnly) = validation.Value;
        var query = _sigiloPayCredentials.AsQueryable();

        if (enabledOnly == true)
            query = query.Where(c => c.Enabled);
        else if (enabledOnly == false)
            query = query.Where(c => !c.Enabled);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.ToLowerInvariant();
            query = query.Where(c =>
                c.Id.ToLower().Contains(term)
                || c.Name.ToLower().Contains(term)
                || c.PublicKey.ToLower().Contains(term)
                || c.SecretKey.ToLower().Contains(term)
                || (c.StrawManId != null && c.StrawManId.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Name)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync();

        return Result<SearchGatewayCredentialsResponse>.Success(new SearchGatewayCredentialsResponse
        {
            Total = total,
            Items = items,
        });
    }

    private static IResult<(int Limit, int Offset, string? Keyword, bool? EnabledOnly)> ValidateSearchRequest(
        SearchGatewayCredentialsRequest? request)
    {
        request ??= new SearchGatewayCredentialsRequest();

        var limit = request.Limit <= 0 ? 30 : request.Limit;
        var offset = request.Offset;
        var keyword = request.Keyword?.Trim();

        if (limit < 0 || limit >= 1000)
        {
            return Result<(int, int, string?, bool?)>.Failure(Error.Create()
                .WithCode(GatewayAdministratorErrorCodes.SearchLimitInvalid)
                .WithMessage("O limite deve estar entre 1 e 999.")
                .Build());
        }

        if (offset < 0)
        {
            return Result<(int, int, string?, bool?)>.Failure(Error.Create()
                .WithCode(GatewayAdministratorErrorCodes.SearchOffsetInvalid)
                .WithMessage("O deslocamento não pode ser negativo.")
                .Build());
        }

        if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length > SearchKeywordMaxLength)
        {
            return Result<(int, int, string?, bool?)>.Failure(Error.Create()
                .WithCode(GatewayAdministratorErrorCodes.SearchKeywordTooLong)
                .WithMessage("A palavra-chave pode ter no máximo 200 caracteres.")
                .Build());
        }

        return Result<(int, int, string?, bool?)>.Success((limit, offset, keyword, request.EnabledOnly));
    }
}
