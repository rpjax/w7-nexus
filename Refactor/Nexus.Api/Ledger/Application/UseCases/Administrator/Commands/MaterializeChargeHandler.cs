using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Shared;
using Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim;
using Refactor.Nexus.Api.Ledger.Domain.Errors;
using Refactor.Nexus.Api.Ledger.Domain.Services;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;
using ClaimAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim.Claim;

namespace Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Commands;

public sealed record MaterializeChargeCommand(
    string ChargeId,
    decimal NetAmount,
    string? Currency,
    string LandingWorldAccountId);

public sealed record MaterializeChargeResult(Guid ChargeId, string Status, IReadOnlyList<Guid> ClaimIds);

public interface IMaterializeChargeUseCase
{
    Task<IOperationResult<MaterializeChargeResult>> HandleAsync(
        MaterializeChargeCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class MaterializeChargeHandler : IMaterializeChargeUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly ILedgerAccess _access;
    private readonly IChargeRepository _charges;
    private readonly IWorldAccountRepository _accounts;
    private readonly IClaimRepository _claims;
    private readonly IMaterializationCommit _commit;

    public MaterializeChargeHandler(
        IRequestContext requestContext,
        ILedgerAccess access,
        IChargeRepository charges,
        IWorldAccountRepository accounts,
        IClaimRepository claims,
        IMaterializationCommit commit)
    {
        _requestContext = requestContext;
        _access = access;
        _charges = charges;
        _accounts = accounts;
        _claims = claims;
        _commit = commit;
    }

    public async Task<IOperationResult<MaterializeChargeResult>> HandleAsync(
        MaterializeChargeCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<MaterializeChargeResult>.Failure(LedgerGuards.BodyRequired());

        var auth = await LedgerGuards.AuthorizeAsync<MaterializeChargeResult>(_requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        if (!Guid.TryParse(command.ChargeId, out var chargeId))
            return Fail(LedgerErrorCodes.ChargeNotFound, "Cobrança invalida.");

        var charge = await _charges.GetByIdAsync(chargeId, cancellationToken);
        if (charge is null)
            return Fail(LedgerErrorCodes.ChargeNotFound, "Cobrança nao encontrada.");

        var currency = string.IsNullOrWhiteSpace(command.Currency)
            ? charge.Currency
            : command.Currency.Trim().ToUpperInvariant();

        if (!Guid.TryParse(command.LandingWorldAccountId, out var landingId))
            return Fail(LedgerErrorCodes.AccountNotFound, "Conta de aterrissagem invalida.");

        if (charge.IsMaterialized)
        {
            var marked = charge.MarkMaterialized(command.NetAmount, currency, landingId);
            if (marked.IsFailure)
                return OperationResult<MaterializeChargeResult>.Failure(marked.Errors);

            var existing = await _claims.ListAsync(charge.Id, null, null, cancellationToken);
            return OperationResult<MaterializeChargeResult>.Success(
                new MaterializeChargeResult(charge.Id, charge.Status.ToString(), existing.Select(c => c.Id).ToList()));
        }

        var account = await _accounts.GetByIdAsync(landingId, cancellationToken);
        if (account is null)
            return Fail(LedgerErrorCodes.AccountNotFound, "Conta de aterrissagem nao encontrada.");

        if (account.BalanceStatus == BalanceStatus.Lost)
            return Fail(LedgerErrorCodes.LandingLost, "Conta com saldo perdido nao recebe aterrissagem.");

        var located = await _claims.ListAsync(null, landingId, null, cancellationToken);
        var activeSum = located.Where(c => c.IsActive && string.Equals(c.Currency, currency, StringComparison.OrdinalIgnoreCase))
            .Sum(c => c.Amount);
        if (activeSum != account.BalanceOf(currency))
            return Fail(LedgerErrorCodes.InvariantBroken, "Saldo da Conta nao casa com claims ativos nesta moeda.");

        var materialized = charge.MarkMaterialized(command.NetAmount, currency, landingId);
        if (materialized.IsFailure)
            return OperationResult<MaterializeChargeResult>.Failure(materialized.Errors);

        var credited = account.Credit(currency, command.NetAmount, $"materialize:{charge.Id:N}");
        if (credited.IsFailure)
            return OperationResult<MaterializeChargeResult>.Failure(credited.Errors);

        var slices = WaterfallMaterializer.Allocate(charge.SplitIntent, command.NetAmount);
        if (slices.Sum(s => s.Amount) != command.NetAmount)
            return Fail(LedgerErrorCodes.SplitFailed, "Waterfall nao soma o liquido X.");

        var opened = new List<ClaimAggregate>();
        foreach (var slice in slices)
        {
            var claim = ClaimAggregate.Open(
                slice.BeneficiaryId,
                slice.Amount,
                currency,
                charge.Id,
                landingId,
                slice.Kind);
            if (claim.IsFailure)
                return OperationResult<MaterializeChargeResult>.Failure(claim.Errors);
            opened.Add(claim.Value!);
        }

        var afterClaims = activeSum + opened.Sum(c => c.Amount);
        if (afterClaims != account.BalanceOf(currency))
            return Fail(LedgerErrorCodes.InvariantBroken, "Invariante soma claims != saldo apos materializacao.");

        await _commit.SaveAsync(charge, account, opened, cancellationToken);
        return OperationResult<MaterializeChargeResult>.Success(
            new MaterializeChargeResult(charge.Id, charge.Status.ToString(), opened.Select(c => c.Id).ToList()));
    }

    private static IOperationResult<MaterializeChargeResult> Fail(string code, string message) =>
        OperationResult<MaterializeChargeResult>.Failure(Error.Create().WithCode(code).WithMessage(message).Build());
}
