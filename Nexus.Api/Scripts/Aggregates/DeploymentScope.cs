using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Scripts.Errors;

namespace Nexus.Scripts.Aggregates;

public sealed class DeploymentScope
{
    private readonly List<HostPattern> _patterns;

    public IReadOnlyList<HostPattern> Patterns => _patterns.AsReadOnly();

    private DeploymentScope(IEnumerable<HostPattern> patterns)
    {
        _patterns = patterns.ToList();
    }

    public static IResult<DeploymentScope> Create(IEnumerable<string>? patternValues)
    {
        var values = patternValues?.ToArray() ?? Array.Empty<string>();

        if (values.Length == 0)
            return Result<DeploymentScope>.Failure(Error.Create()
                .WithCode(ScriptErrorCodes.ScopeInvalid)
                .WithMessage("O escopo de deploy deve conter ao menos um host.")
                .Build());

        var patterns = new List<HostPattern>();

        foreach (var value in values)
        {
            var patternResult = HostPattern.Create(value);
            if (patternResult.IsFailure)
                return Result<DeploymentScope>.Failure(patternResult.Errors);

            patterns.Add(patternResult.Value!);
        }

        return Result<DeploymentScope>.Success(new DeploymentScope(patterns));
    }

    public bool Matches(string requestHost)
    {
        var host = HostPattern.NormalizeHost(requestHost);
        return _patterns.Any(pattern => pattern.Matches(host));
    }
}
