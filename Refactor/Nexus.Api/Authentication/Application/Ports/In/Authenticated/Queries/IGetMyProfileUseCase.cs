using Refactor.Nexus.Api.Authentication.Application.UseCases.Authenticated.Queries.GetMyProfile;

namespace Refactor.Nexus.Api.Authentication.Application.Ports.In.Authenticated.Queries;

public interface IGetMyProfileUseCase
{
    Task<IOperationResult<GetMyProfileResult>> HandleAsync(
        GetMyProfileQuery query,
        CancellationToken cancellationToken = default);
}
