using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Shared;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Queries.GetMemberMandate;

public sealed record GetMemberMandateQuery(string AccountId);

public sealed record MandateGrantView(
    Guid Id,
    string Capability,
    string ScopeKind,
    IReadOnlyList<Guid> OperationIds,
    string GrantedBy,
    DateTime GrantedAt,
    string? SourcePreset);

public sealed record GetMemberMandateResult(
    string AccountId,
    IReadOnlyList<string> AppliedPresets,
    IReadOnlyList<MandateGrantView> Grants);

public interface IGetMemberMandateUseCase
{
    Task<IOperationResult<GetMemberMandateResult>> HandleAsync(
        GetMemberMandateQuery query,
        CancellationToken cancellationToken = default);
}

public sealed class GetMemberMandateHandler : IGetMemberMandateUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateAccessPolicy _accessPolicy;
    private readonly IMemberMandateReadRepository _mandateReadRepository;

    public GetMemberMandateHandler(
        IRequestContext requestContext,
        IMandateAccessPolicy accessPolicy,
        IMemberMandateReadRepository mandateReadRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _mandateReadRepository = mandateReadRepository;
    }

    public async Task<IOperationResult<GetMemberMandateResult>> HandleAsync(
        GetMemberMandateQuery query,
        CancellationToken cancellationToken = default)
    {
        var access = await MandateAdministratorGuards.AuthorizeAdminAsync<GetMemberMandateResult>(
            _requestContext, _accessPolicy, cancellationToken);
        if (access is not null)
            return access;

        if (!MemberId.TryParse(query.AccountId, out var memberId))
            return OperationResult<GetMemberMandateResult>.Failure(MandateAdministratorGuards.AccountNotFound(query.AccountId));

        var mandate = await _mandateReadRepository.GetByMemberIdAsync(memberId, cancellationToken)
            ?? MemberMandate.Empty(memberId);

        return OperationResult<GetMemberMandateResult>.Success(new GetMemberMandateResult(
            memberId.ToString(),
            mandate.AppliedPresets.OrderBy(x => x).ToList(),
            mandate.Grants.Select(g => new MandateGrantView(
                g.Id,
                g.Capability,
                g.Scope.Kind.ToString(),
                g.Scope.OperationIds,
                g.GrantedBy.ToString(),
                g.GrantedAt,
                g.SourcePreset)).ToList()));
    }
}
