using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;

namespace Nexus.Administrators.Application.Services;

internal static class StrawManValidation
{
    public static async Task<IResult?> ValidateStrawManAccountAsync(
        IAccountRepository accounts,
        string strawManId,
        string invalidCode,
        string notFoundCode,
        string roleRequiredCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(strawManId))
            return Result.Failure(Error.Create()
                .WithCode(invalidCode)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        var trimmedId = strawManId.Trim();
        var account = await accounts.AsQueryable()
            .Where(a => a.Id == trimmedId)
            .FirstOrDefaultAsync();

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
