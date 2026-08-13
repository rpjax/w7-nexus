using Refactor.Nexus.Api.Authentication.Application.Ports.In.Authenticated.Queries;
using Refactor.Nexus.Api.Authentication.Application.Ports.In.Unauthenticated.Commands;
using Refactor.Nexus.Api.Authentication.Application.Ports.Out.Tokens;
using Refactor.Nexus.Api.Authentication.Application.UseCases.Authenticated.Queries.GetMyProfile;
using Refactor.Nexus.Api.Authentication.Application.UseCases.Unauthenticated.Commands.SignIn;
using Refactor.Nexus.Api.Authentication.Application.UseCases.Unauthenticated.Commands.SignUpAdmin;
using Refactor.Nexus.Api.Authentication.Infrastructure.Tokens;

namespace Refactor.Nexus.Api.Authentication.Composition;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddRefactorAuthentication(this IServiceCollection services)
    {
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddScoped<ISignUpAdminUseCase, SignUpAdminHandler>();
        services.AddScoped<ISignInUseCase, SignInHandler>();
        services.AddScoped<IGetMyProfileUseCase, GetMyProfileHandler>();

        return services;
    }
}
