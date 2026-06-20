using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.Withdrawals.Application.Contracts;
using Nexus.Withdrawals.Errors;

namespace Nexus.Withdrawals.Application.Services;

internal static class StrawManValidation
{
    public static IResult? ValidateStrawManAccount(
        IAccountRepository accounts,
        string strawManAccountId,
        string invalidCode,
        string notFoundCode,
        string roleRequiredCode)
    {
        if (string.IsNullOrWhiteSpace(strawManAccountId))
            return Result.Failure(Error.Create()
                .WithCode(invalidCode)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        var account = accounts.AsQueryable()
            .FirstOrDefault(a => a.Id == strawManAccountId);

        if (account is null)
            return Result.Failure(Error.Create()
                .WithCode(notFoundCode)
                .WithMessage($"A conta laranja '{strawManAccountId}' não foi encontrada.")
                .Build());

        if (!account.Roles.Contains(Roles.StrawMan, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(roleRequiredCode)
                .WithMessage($"A conta '{strawManAccountId}' não possui o perfil de laranja.")
                .Build());

        return null;
    }
}
