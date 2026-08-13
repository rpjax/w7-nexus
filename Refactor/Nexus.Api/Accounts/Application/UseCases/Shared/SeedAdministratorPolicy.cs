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
        string? handle,
        string? password,
        bool creationTokenConfigured)
    {
        if (administratorCount > 0)
            return SeedAdministratorDecision.SkipAlreadyHasAdministrator;

        var handleSet = !string.IsNullOrWhiteSpace(handle);
        var passwordSet = !string.IsNullOrWhiteSpace(password);
        if (!handleSet && !passwordSet)
            return SeedAdministratorDecision.SkipNotConfigured;

        if (!creationTokenConfigured)
            return SeedAdministratorDecision.SkipMissingCreationTokenGuard;

        if (!handleSet || !passwordSet)
            return SeedAdministratorDecision.SkipNotConfigured;

        return SeedAdministratorDecision.Seed;
    }
}
