using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Mandates.Domain.Errors;
using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Shared;

internal static class MandateScopeParser
{
    public static IResult<MandateScope> Parse(string? scopeKind, IReadOnlyList<Guid>? operationIds = null)
    {
        if (string.IsNullOrWhiteSpace(scopeKind))
        {
            return Result<MandateScope>.Failure(Error.Create()
                .WithCode(MandateErrorCodes.CapabilityEmpty)
                .WithMessage("ScopeKind obrigatorio.")
                .Build());
        }

        if (!Enum.TryParse<MandateScopeKind>(scopeKind.Trim(), ignoreCase: true, out var kind))
        {
            return Result<MandateScope>.Failure(Error.Create()
                .WithCode(MandateErrorCodes.CapabilityUnknown)
                .WithMessage($"ScopeKind '{scopeKind}' desconhecido.")
                .Build());
        }

        return kind switch
        {
            MandateScopeKind.Organization => Result<MandateScope>.Success(MandateScope.Organization()),
            MandateScopeKind.CarteiraDirect => Result<MandateScope>.Success(MandateScope.CarteiraDirect()),
            MandateScopeKind.OperationNone => Result<MandateScope>.Success(MandateScope.OperationNone()),
            MandateScopeKind.OperationAll => Result<MandateScope>.Success(MandateScope.OperationAll()),
            MandateScopeKind.OperationSpecific => MandateScope.OperationSpecific(operationIds ?? []),
            _ => Result<MandateScope>.Failure(Error.Create()
                .WithCode(MandateErrorCodes.CapabilityUnknown)
                .WithMessage($"ScopeKind '{scopeKind}' desconhecido.")
                .Build())
        };
    }
}
