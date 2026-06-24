using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.AccountNodes.Application.Contracts;
using Nexus.AccountNodes.Errors;

namespace Nexus.AccountNodes.Application.Services;

internal static class StrawManValidation
{
    public static IResult? ValidateStrawManAccount(
        IAccountRepository accounts,
        string strawManId,
        string invalidCode,
        string notFoundCode,
        string roleRequiredCode)
    {
        if (string.IsNullOrWhiteSpace(strawManId))
            return Result.Failure(Error.Create()
                .WithCode(invalidCode)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        var account = accounts.AsQueryable()
            .FirstOrDefault(a => a.Id == strawManId);

        if (account is null)
            return Result.Failure(Error.Create()
                .WithCode(notFoundCode)
                .WithMessage($"A conta laranja '{strawManId}' não foi encontrada.")
                .Build());

        if (!account.Roles.Contains(Roles.StrawMan, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(roleRequiredCode)
                .WithMessage($"A conta '{strawManId}' não possui o perfil de laranja.")
                .Build());

        return null;
    }
}
