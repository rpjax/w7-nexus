using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Shared;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate;
using Refactor.Nexus.Api.Mandates.Domain.Errors;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.RecordMemberAttrition;

public sealed record RecordMemberAttritionCommand(string AccountId, string Status, string Cause);
public sealed class RecordMemberAttritionResult;

public interface IRecordMemberAttritionUseCase
{
    Task<IOperationResult<RecordMemberAttritionResult>> HandleAsync(
        RecordMemberAttritionCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class RecordMemberAttritionHandler : IRecordMemberAttritionUseCase
{
    private static readonly HashSet<string> Causes = new(StringComparer.OrdinalIgnoreCase)
    {
        "bloqueio_bancario", "apreensao", "traicao", "saida_voluntaria",
        "erro_operacional", "estorno", "desconhecido"
    };

    private readonly IRequestContext _requestContext;
    private readonly IMandateAccessPolicy _accessPolicy;
    private readonly IAccountDirectory _accountDirectory;
    private readonly IMemberMandateRepository _mandateRepository;
    private readonly IMemberMandateReadRepository _mandateReadRepository;
    private readonly IAgencyDealRepository _agencyDealRepository;
    private readonly IAgencyDealReadRepository _agencyDealReadRepository;

    public RecordMemberAttritionHandler(
        IRequestContext requestContext,
        IMandateAccessPolicy accessPolicy,
        IAccountDirectory accountDirectory,
        IMemberMandateRepository mandateRepository,
        IMemberMandateReadRepository mandateReadRepository,
        IAgencyDealRepository agencyDealRepository,
        IAgencyDealReadRepository agencyDealReadRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _accountDirectory = accountDirectory;
        _mandateRepository = mandateRepository;
        _mandateReadRepository = mandateReadRepository;
        _agencyDealRepository = agencyDealRepository;
        _agencyDealReadRepository = agencyDealReadRepository;
    }

    public async Task<IOperationResult<RecordMemberAttritionResult>> HandleAsync(
        RecordMemberAttritionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<RecordMemberAttritionResult>.Failure(MandateAdministratorGuards.RequestBodyRequired());

        var (requester, access) = await MandateAdministratorGuards.AuthorizeAdminWithRequesterAsync<RecordMemberAttritionResult>(
            _requestContext, _accessPolicy, cancellationToken);
        if (access is not null)
            return access;

        if (!MemberId.TryParse(command.AccountId, out var memberId))
            return OperationResult<RecordMemberAttritionResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.AccountId));

        if (!await _accountDirectory.ExistsAsync(memberId, cancellationToken))
            return OperationResult<RecordMemberAttritionResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.AccountId));

        if (!Causes.Contains((command.Cause ?? "").Trim()))
        {
            return OperationResult<RecordMemberAttritionResult>.Failure(
                Error.Create()
                    .WithCode(MandateErrorCodes.AttritionInvalid)
                    .WithMessage("Causa incompatível com o estado.")
                    .Build());
        }

        var mandate = await _mandateRepository.GetByMemberIdAsync(memberId, cancellationToken)
            ?? MemberMandate.Empty(memberId);
        var recorded = mandate.RecordAttrition(command.Status, command.Cause ?? "");
        if (recorded.IsFailure)
            return OperationResult<RecordMemberAttritionResult>.Failure(recorded.Errors);

        await _mandateRepository.SaveAsync(mandate, cancellationToken);

        var status = mandate.AttritionStatus;
        if (status is "burned" or "betrayed")
            await DropIssuedTreeAsync(memberId, cancellationToken);
        else if (status is "left")
        {
            var actor = new MemberId(Guid.Parse(requester!.AccountId));
            var concedente = mandate.TryGetConcedente(out var parent) ? parent : actor;
            await ReparentDownlineAsync(memberId, concedente, cancellationToken);
        }

        return OperationResult<RecordMemberAttritionResult>.Success(new RecordMemberAttritionResult());
    }

    /// <summary>
    /// Queimado/traiu: drop grants issued by the member; recurse into downline that has no remaining grants.
    /// </summary>
    private async Task DropIssuedTreeAsync(MemberId grantorId, CancellationToken cancellationToken)
    {
        var queue = new Queue<MemberId>();
        queue.Enqueue(grantorId);
        var seen = new HashSet<Guid> { grantorId.Value };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var dependents = await _mandateReadRepository.ListGrantedByAsync(current, cancellationToken);
            foreach (var dependent in dependents)
            {
                var dropped = dependent.DropGrantsIssuedBy(current);
                if (dropped > 0)
                    await _mandateRepository.SaveAsync(dependent, cancellationToken);

                if (dependent.Grants.Count == 0 && seen.Add(dependent.MemberId.Value))
                    queue.Enqueue(dependent.MemberId);
            }
        }
    }

    /// <summary>
    /// Saída voluntária: sub-mandatos and carteira (AgencyDeal) sobem para o concedente. Sem absorção Org.
    /// </summary>
    private async Task ReparentDownlineAsync(
        MemberId departingId,
        MemberId concedente,
        CancellationToken cancellationToken)
    {
        var dependents = await _mandateReadRepository.ListGrantedByAsync(departingId, cancellationToken);
        foreach (var dependent in dependents)
        {
            if (dependent.ReparentGrantsIssuedBy(departingId, concedente) > 0)
                await _mandateRepository.SaveAsync(dependent, cancellationToken);
        }

        var deals = await _agencyDealReadRepository.ListActiveByRecruiterAsync(departingId, cancellationToken);
        foreach (var deal in deals)
        {
            if (concedente.Equals(deal.OperatorId))
                continue;

            var moved = deal.UpdatePercents(deal.OperatorPercent, deal.RecruiterPercent, concedente);
            if (moved.IsSuccess)
                await _agencyDealRepository.SaveAsync(deal, cancellationToken);
        }
    }
}
