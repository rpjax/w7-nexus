namespace Refactor.Nexus.Api.Accounts.Application.UseCases.Shared;

public enum SeedAdministratorDecision
{
    SkipAlreadyHasAdministrator,
    SkipNotConfigured,
    SkipMissingCreationTokenGuard,
    Seed
}

public static class SeedAdministratorPolicy
{
    public static SeedAdministratorDecision Evaluate(
        int administratorCount,
        string? username,
        string? password,
        bool creationTokenConfigured)
    {
        if (administratorCount > 0)
            return SeedAdministratorDecision.SkipAlreadyHasAdministrator;

        var usernameSet = !string.IsNullOrWhiteSpace(username);
        var passwordSet = !string.IsNullOrWhiteSpace(password);
        if (!usernameSet && !passwordSet)
            return SeedAdministratorDecision.SkipNotConfigured;

        if (!creationTokenConfigured)
            return SeedAdministratorDecision.SkipMissingCreationTokenGuard;

        if (!usernameSet || !passwordSet)
            return SeedAdministratorDecision.SkipNotConfigured;

        return SeedAdministratorDecision.Seed;
    }
}
