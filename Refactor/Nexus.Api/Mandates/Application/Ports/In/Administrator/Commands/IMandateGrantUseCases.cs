using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.GrantPreset;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.RevokePreset;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.GrantCapability;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.RevokeCapability;

namespace Refactor.Nexus.Api.Mandates.Application.Ports.In.Administrator.Commands;

public interface IGrantPresetUseCase
{
    Task<IOperationResult<GrantPresetResult>> HandleAsync(
        GrantPresetCommand command,
        CancellationToken cancellationToken = default);
}

public interface IRevokePresetUseCase
{
    Task<IOperationResult<RevokePresetResult>> HandleAsync(
        RevokePresetCommand command,
        CancellationToken cancellationToken = default);
}

public interface IGrantCapabilityUseCase
{
    Task<IOperationResult<GrantCapabilityResult>> HandleAsync(
        GrantCapabilityCommand command,
        CancellationToken cancellationToken = default);
}

public interface IRevokeCapabilityUseCase
{
    Task<IOperationResult<RevokeCapabilityResult>> HandleAsync(
        RevokeCapabilityCommand command,
        CancellationToken cancellationToken = default);
}
