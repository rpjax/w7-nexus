using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Operations;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Shared;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.Errors;
using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.GrantCapability;

public sealed record GrantCapabilityCommand(
    string AccountId,
    string Capability,
    string ScopeKind,
    IReadOnlyList<Guid>? OperationIds = null);

public sealed class GrantCapabilityResult;

public sealed class GrantCapabilityHandler : IGrantCapabilityUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateAccessPolicy _accessPolicy;
    private readonly IAccountDirectory _accountDirectory;
    private readonly IMemberMandateRepository _mandateRepository;
    private readonly IMemberMandateReadRepository _mandateReadRepository;
    private readonly IOperationDirectory _operationDirectory;
    private readonly IOperationAssignmentProbe _assignmentProbe;

    public GrantCapabilityHandler(
        IRequestContext requestContext,
        IMandateAccessPolicy accessPolicy,
        IAccountDirectory accountDirectory,
        IMemberMandateRepository mandateRepository,
        IMemberMandateReadRepository mandateReadRepository,
        IOperationDirectory operationDirectory,
        IOperationAssignmentProbe assignmentProbe)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _accountDirectory = accountDirectory;
        _mandateRepository = mandateRepository;
        _mandateReadRepository = mandateReadRepository;
        _operationDirectory = operationDirectory;
        _assignmentProbe = assignmentProbe;
    }

    public async Task<IOperationResult<GrantCapabilityResult>> HandleAsync(
        GrantCapabilityCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<GrantCapabilityResult>.Failure(MandateAdministratorGuards.RequestBodyRequired());

        var (requester, failure) = await MandateAdministratorGuards.AuthorizeAdminWithRequesterAsync<GrantCapabilityResult>(
            _requestContext, _accessPolicy, cancellationToken);
        if (failure is not null)
            return failure;

        if (!MemberId.TryParse(command.AccountId, out var memberId))
            return OperationResult<GrantCapabilityResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.AccountId));

        if (!await _accountDirectory.ExistsAsync(memberId, cancellationToken))
            return OperationResult<GrantCapabilityResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.AccountId));

        var scopeResult = MandateScopeParser.Parse(command.ScopeKind, command.OperationIds);
        if (scopeResult.IsFailure)
            return OperationResult<GrantCapabilityResult>.Failure(scopeResult.Errors);

        var scope = scopeResult.Value!;
        if (scope.Kind == MandateScopeKind.OperationSpecific)
        {
            foreach (var opId in scope.OperationIds)
            {
                if (!await _operationDirectory.ExistsAsync(opId, cancellationToken))
                {
                    return OperationResult<GrantCapabilityResult>.Failure(Error.Create()
                        .WithCode(MandateErrorCodes.OperationNotFound)
                        .WithMessage($"Operacao '{opId}' nao encontrada.")
                        .Build());
                }
            }
        }

        if (string.Equals(command.Capability.Trim(), Capabilities.GerirOperacao, StringComparison.Ordinal))
        {
            var conflict = await EnsureNotTipOnManagedOpsAsync(memberId, scope, cancellationToken);
            if (conflict is not null)
                return OperationResult<GrantCapabilityResult>.Failure(conflict);
        }

        var grantorId = new MemberId(Guid.Parse(requester!.AccountId));
        var grantorIsAdmin = await _accountDirectory.IsAdministratorAsync(grantorId, cancellationToken);
        var grantorMandate = grantorIsAdmin
            ? null
            : await _mandateReadRepository.GetByMemberIdAsync(grantorId, cancellationToken)
              ?? MemberMandate.Empty(grantorId);

        var mandate = await _mandateRepository.GetByMemberIdAsync(memberId, cancellationToken)
            ?? MemberMandate.Empty(memberId);

        var mutation = mandate.GrantCapability(
            command.Capability,
            scope,
            grantorId,
            grantorIsAdmin,
            grantorMandate);
        if (mutation.IsFailure)
            return OperationResult<GrantCapabilityResult>.Failure(mutation.Errors);

        await _mandateRepository.SaveAsync(mandate, cancellationToken);
        return OperationResult<GrantCapabilityResult>.Success(new GrantCapabilityResult());
    }

    private async Task<Error?> EnsureNotTipOnManagedOpsAsync(
        MemberId memberId,
        MandateScope scope,
        CancellationToken cancellationToken)
    {
        if (scope.Kind is MandateScopeKind.Organization or MandateScopeKind.OperationAll)
        {
            if (await _assignmentProbe.IsMemberAssignedToAnyAsync(memberId.Value, cancellationToken))
            {
                return Error.Create()
                    .WithCode(MandateErrorCodes.TipManagementConflict)
                    .WithMessage("Conflito ponta x gestao: membro ja e Operador assigned em alguma Operacao.")
                    .Build();
            }

            return null;
        }

        if (scope.Kind == MandateScopeKind.OperationSpecific)
        {
            foreach (var opId in scope.OperationIds)
            {
                if (await _assignmentProbe.IsMemberAssignedAsync(opId, memberId.Value, cancellationToken))
                {
                    return Error.Create()
                        .WithCode(MandateErrorCodes.TipManagementConflict)
                        .WithMessage("Conflito ponta x gestao: membro ja e Operador assigned nesta Operacao.")
                        .Build();
                }
            }
        }

        return null;
    }
}
