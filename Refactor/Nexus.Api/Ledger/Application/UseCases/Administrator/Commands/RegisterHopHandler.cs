using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Ledger.Application.Journal;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Ledger.Application.UseCases.Shared;
using Refactor.Nexus.Api.Ledger.Domain;
using Refactor.Nexus.Api.Ledger.Domain.Errors;
using Refactor.Nexus.Api.Ledger.Domain.Events;
using Refactor.Nexus.Api.Ledger.Domain.Services;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;
using ClaimAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim.Claim;
using HopAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Hop.Hop;
using WorldAccountAggregate = Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount.WorldAccount;

namespace Refactor.Nexus.Api.Ledger.Application.UseCases.Administrator.Commands;

public sealed record HopDestinationInput(string AccountId, decimal Amount, string Currency);

public sealed record HopCutInput(string OrangeMemberId, decimal Percent, bool InPlace, string? OrangeAccountId);

public sealed record RegisterHopCommand(
    string OriginAccountId,
    string Currency,
    IReadOnlyList<string>? ClaimIds,
    IReadOnlyList<HopDestinationInput> Destinations,
    HopCutInput? Cut,
    bool KeepRemainderAtOrigin = false,
    string? LossCause = null);

public sealed record RegisterHopResult(Guid HopId, decimal LossAmount, IReadOnlyList<Guid> ClaimIds);

public interface IRegisterHopUseCase
{
    Task<IOperationResult<RegisterHopResult>> HandleAsync(
        RegisterHopCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class RegisterHopHandler : IRegisterHopUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly ILedgerAccess _access;
    private readonly IChargeRepository _charges;
    private readonly IWorldAccountRepository _accounts;
    private readonly IClaimRepository _claims;
    private readonly ILedgerCommit _commit;
    private readonly IJournalWriter _journal;

    public RegisterHopHandler(
        IRequestContext requestContext,
        ILedgerAccess access,
        IChargeRepository charges,
        IWorldAccountRepository accounts,
        IClaimRepository claims,
        ILedgerCommit commit,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _access = access;
        _charges = charges;
        _accounts = accounts;
        _claims = claims;
        _commit = commit;
        _journal = journal;
    }

    public async Task<IOperationResult<RegisterHopResult>> HandleAsync(
        RegisterHopCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<RegisterHopResult>.Failure(LedgerGuards.BodyRequired());

        var auth = await LedgerGuards.AuthorizeAsync<RegisterHopResult>(_requestContext, _access, cancellationToken);
        if (auth.Failure is not null)
            return auth.Failure;

        if (!Guid.TryParse(command.OriginAccountId, out var originId))
            return Fail(LedgerErrorCodes.AccountNotFound, "Conta de origem inválida.");

        var currency = (command.Currency ?? "BRL").Trim().ToUpperInvariant();
        var origin = await _accounts.GetByIdAsync(originId, cancellationToken);
        if (origin is null)
            return Fail(LedgerErrorCodes.AccountNotFound, "Conta de origem nao encontrada.");
        if (origin.BalanceStatus == BalanceStatus.Lost)
            return Fail(LedgerErrorCodes.AccountLost, "Conta com saldo perdido nao move.");

        var located = await _claims.ListAsync(null, originId, null, cancellationToken);
        var activeHere = located
            .Where(c => c.IsActive && string.Equals(c.Currency, currency, StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<ClaimAggregate> bundleClaims;
        if (command.ClaimIds is { Count: > 0 })
        {
            bundleClaims = [];
            foreach (var raw in command.ClaimIds)
            {
                if (!Guid.TryParse(raw, out var claimId))
                    return Fail(LedgerErrorCodes.ClaimNotFound, "Claim invalido.");
                var claim = await _claims.GetByIdAsync(claimId, cancellationToken)
                    ?? activeHere.FirstOrDefault(c => c.Id == claimId);
                if (claim is null)
                    return Fail(LedgerErrorCodes.ClaimNotFound, "Claim nao encontrado.");
                if (!claim.IsActive)
                    return Fail(LedgerErrorCodes.ClaimNotActive, "Claim nao esta ativo.");
                if (claim.LocationAccountId != originId
                    || !string.Equals(claim.Currency, currency, StringComparison.OrdinalIgnoreCase))
                    return Fail(LedgerErrorCodes.HopInvalid, "Claim fora da origem/moeda do hop.");
                bundleClaims.Add(claim);
            }
        }
        else
        {
            bundleClaims = activeHere;
        }

        if (bundleClaims.Count == 0)
            return Fail(LedgerErrorCodes.BundleEmpty, "Bundle vazio.");

        HopCutSpec? cut = null;
        if (command.Cut is not null)
        {
            if (!Guid.TryParse(command.Cut.OrangeMemberId, out var orangeId))
                return Fail(LedgerErrorCodes.OrangeNotEligible, "Laranja invalido.");
            if (!await _access.IsEligibleOrangeAsync(orangeId, cancellationToken))
                return Fail(LedgerErrorCodes.OrangeNotEligible, "Membro nao e Laranja elegivel.");

            Guid? orangeAccountId = null;
            if (!command.Cut.InPlace)
            {
                if (!Guid.TryParse(command.Cut.OrangeAccountId, out var parsedOrangeAccount))
                    return Fail(LedgerErrorCodes.AccountNotFound, "Conta do Laranja invalida.");
                orangeAccountId = parsedOrangeAccount;
            }

            var origins = bundleClaims.Select(c => c.OriginChargeId).Distinct().ToList();
            foreach (var originChargeId in origins)
            {
                var charge = await _charges.GetByIdAsync(originChargeId, cancellationToken);
                if (charge is not null && charge.OrangeMemberId == orangeId)
                    return Fail(LedgerErrorCodes.CutAlreadyTaken, "Cut de emissao deste Laranja ja conta neste fluxo.");

                var existing = await _claims.ListAsync(originChargeId, null, orangeId, cancellationToken);
                if (existing.Any(c => c.Kind == ClaimAggregate.PathCutKind && c.BeneficiaryId == orangeId))
                    return Fail(LedgerErrorCodes.CutAlreadyTaken, "Cut mid-path deste Laranja ja existe neste fluxo.");
            }

            cut = new HopCutSpec(orangeId, command.Cut.Percent, command.Cut.InPlace, orangeAccountId);
        }

        var destInputs = command.Destinations ?? [];
        var destSpecs = new List<HopDestSpec>();
        foreach (var dest in destInputs)
        {
            if (!Guid.TryParse(dest.AccountId, out var destId))
                return Fail(LedgerErrorCodes.AccountNotFound, "Conta de destino invalida.");
            if (destId == originId)
                return Fail(LedgerErrorCodes.HopInvalid, "Origem e destino não podem ser a mesma conta.");
            destSpecs.Add(new HopDestSpec(destId, dest.Amount, dest.Currency));
        }

        var bundle = bundleClaims.Select(c => new HopBundleItem(
            c.Id,
            c.BeneficiaryId,
            c.Amount,
            c.OriginChargeId,
            c.LocationAccountId,
            c.Kind,
            c.Currency,
            c.BirthAmount,
            c.BirthCurrency)).ToList();

        var planned = HopAllocator.Plan(bundle, destSpecs, cut, command.KeepRemainderAtOrigin);
        if (planned.IsFailure)
            return OperationResult<RegisterHopResult>.Failure(planned.Errors);

        var plan = planned.Value!;
        if (plan.LossAmount > 0 && !AttritionCause.TryNormalize(command.LossCause, out _))
            return Fail(LedgerErrorCodes.CauseRequired, "Hop com perda exige causa. Informe a causa ou mantenha o resto na origem.");
        var accountsById = new Dictionary<Guid, WorldAccountAggregate> { [origin.Id] = origin };
        var claimsById = bundleClaims.ToDictionary(c => c.Id);
        var touchedClaims = new List<ClaimAggregate>();

        foreach (var move in plan.WorldMoves)
        {
            if (!accountsById.TryGetValue(move.AccountId, out var account))
            {
                var loaded = await _accounts.GetByIdAsync(move.AccountId, cancellationToken);
                if (loaded is null)
                    return Fail(LedgerErrorCodes.AccountNotFound, "Conta do hop nao encontrada.");
                accountsById[loaded.Id] = loaded;
                account = loaded;
            }

            if (account.BalanceStatus == BalanceStatus.Lost)
                return Fail(LedgerErrorCodes.AccountLost, "Conta com saldo perdido nao move.");

            if (move.Amount <= 0)
                continue;

            var applied = move.Credit
                ? account.Credit(move.Currency, move.Amount, "hop")
                : account.Debit(move.Currency, move.Amount, "hop");
            if (applied.IsFailure)
                return OperationResult<RegisterHopResult>.Failure(applied.Errors);
        }

        foreach (var archiveId in plan.Archives)
        {
            if (!claimsById.TryGetValue(archiveId, out var claim))
                return Fail(LedgerErrorCodes.ClaimNotFound, "Claim do bundle nao encontrado.");
            var archived = claim.Archive();
            if (archived.IsFailure)
                return OperationResult<RegisterHopResult>.Failure(archived.Errors);
            touchedClaims.Add(claim);
        }

        foreach (var (claimId, amount, claimCurrency, location) in plan.Adjustments)
        {
            if (!claimsById.TryGetValue(claimId, out var claim))
                return Fail(LedgerErrorCodes.ClaimNotFound, "Claim do bundle nao encontrado.");
            var adjusted = claim.Adjust(amount, claimCurrency, location);
            if (adjusted.IsFailure)
                return OperationResult<RegisterHopResult>.Failure(adjusted.Errors);
            touchedClaims.Add(claim);
        }

        foreach (var spec in plan.NewClaims)
        {
            var opened = ClaimAggregate.Open(
                spec.BeneficiaryId,
                spec.Amount,
                spec.Currency,
                spec.OriginChargeId,
                spec.LocationAccountId,
                spec.Kind,
                spec.ParentClaimId,
                spec.BirthAmount,
                spec.BirthCurrency);
            if (opened.IsFailure)
                return OperationResult<RegisterHopResult>.Failure(opened.Errors);
            claimsById[opened.Value!.Id] = opened.Value;
            touchedClaims.Add(opened.Value);
        }

        foreach (var account in accountsById.Values)
        {
            var atAccount = (await _claims.ListAsync(null, account.Id, null, cancellationToken)).ToList();
            foreach (var mutated in claimsById.Values)
            {
                atAccount.RemoveAll(c => c.Id == mutated.Id);
                if (mutated.LocationAccountId == account.Id)
                    atAccount.Add(mutated);
            }

            foreach (var accountCurrency in atAccount.Select(c => c.Currency).Distinct(StringComparer.OrdinalIgnoreCase)
                         .Concat(account.Balances.Keys).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var sum = atAccount
                    .Where(c => c.IsActive && string.Equals(c.Currency, accountCurrency, StringComparison.OrdinalIgnoreCase))
                    .Sum(c => c.Amount);
                if (sum != account.BalanceOf(accountCurrency))
                    return Fail(LedgerErrorCodes.InvariantBroken, "Invariante soma claims != saldo apos hop.");
            }
        }

        var hop = HopAggregate.Register(
            originId,
            currency,
            bundleClaims.Select(c => c.Id).ToList(),
            destSpecs.Select(d => new HopDestinationSnapshot(d.AccountId, d.Amount, d.Currency.Trim().ToUpperInvariant())).ToList(),
            cut?.OrangeMemberId,
            cut?.Percent,
            cut?.InPlace ?? false,
            plan.LossAmount);

        await _commit.SaveAsync(accountsById.Values.ToList(), touchedClaims.DistinctBy(c => c.Id).ToList(), hop, null, cancellationToken);
        _journal.RecordHopRegistered(hop.Id, Guid.Parse(auth.Requester!.AccountId));
        return OperationResult<RegisterHopResult>.Success(
            new RegisterHopResult(hop.Id, plan.LossAmount, touchedClaims.Select(c => c.Id).Distinct().ToList()));
    }

    private static IOperationResult<RegisterHopResult> Fail(string code, string message) =>
        OperationResult<RegisterHopResult>.Failure(Error.Create().WithCode(code).WithMessage(message).Build());
}
