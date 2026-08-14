using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Shared;
using Refactor.Nexus.Api.Ledger.Domain.Errors;
using ClaimAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim.Claim;

namespace Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Queries;

public sealed record ClaimView(
    Guid ClaimId,
    Guid BeneficiaryId,
    decimal Amount,
    string Currency,
    Guid OriginChargeId,
    Guid LocationAccountId,
    string Status,
    string Kind,
    DateTime OpenedAt);

public sealed record ListClaimsQuery(string? ChargeId, string? AccountId, string? BeneficiaryId);
public sealed record ListClaimsResult(IReadOnlyList<ClaimView> Items);
public interface IListClaimsUseCase
{
    Task<IOperationResult<ListClaimsResult>> HandleAsync(ListClaimsQuery query, CancellationToken cancellationToken = default);
}

public sealed class ListClaimsHandler : IListClaimsUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly ILedgerAccess _access;
    private readonly IClaimRepository _claims;

    public ListClaimsHandler(IRequestContext requestContext, ILedgerAccess access, IClaimRepository claims)
    {
        _requestContext = requestContext;
        _access = access;
        _claims = claims;
    }

    public async Task<IOperationResult<ListClaimsResult>> HandleAsync(
        ListClaimsQuery query,
        CancellationToken cancellationToken = default)
    {
        var auth = await LedgerGuards.AuthorizeAsync<ListClaimsResult>(_requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        Guid? chargeId = Guid.TryParse(query.ChargeId, out var cid) ? cid : null;
        Guid? accountId = Guid.TryParse(query.AccountId, out var aid) ? aid : null;
        Guid? beneficiaryId = Guid.TryParse(query.BeneficiaryId, out var bid) ? bid : null;

        var items = await _claims.ListAsync(chargeId, accountId, beneficiaryId, cancellationToken);
        return OperationResult<ListClaimsResult>.Success(new ListClaimsResult(items.Select(ToView).ToList()));
    }

    internal static ClaimView ToView(ClaimAggregate claim) =>
        new(
            claim.Id,
            claim.BeneficiaryId,
            claim.Amount,
            claim.Currency,
            claim.OriginChargeId,
            claim.LocationAccountId,
            claim.Status.ToString(),
            claim.Kind,
            claim.OpenedAt);
}

public sealed record GetClaimQuery(string ClaimId);
public interface IGetClaimUseCase
{
    Task<IOperationResult<ClaimView>> HandleAsync(GetClaimQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetClaimHandler : IGetClaimUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly ILedgerAccess _access;
    private readonly IClaimRepository _claims;

    public GetClaimHandler(IRequestContext requestContext, ILedgerAccess access, IClaimRepository claims)
    {
        _requestContext = requestContext;
        _access = access;
        _claims = claims;
    }

    public async Task<IOperationResult<ClaimView>> HandleAsync(
        GetClaimQuery query,
        CancellationToken cancellationToken = default)
    {
        var auth = await LedgerGuards.AuthorizeAsync<ClaimView>(_requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        if (!Guid.TryParse(query.ClaimId, out var id))
        {
            return OperationResult<ClaimView>.Failure(Error.Create()
                .WithCode(LedgerErrorCodes.ClaimNotFound)
                .WithMessage("Claim nao encontrado.")
                .Build());
        }

        var claim = await _claims.GetByIdAsync(id, cancellationToken);
        if (claim is null)
        {
            return OperationResult<ClaimView>.Failure(Error.Create()
                .WithCode(LedgerErrorCodes.ClaimNotFound)
                .WithMessage("Claim nao encontrado.")
                .Build());
        }

        return OperationResult<ClaimView>.Success(ListClaimsHandler.ToView(claim));
    }
}
