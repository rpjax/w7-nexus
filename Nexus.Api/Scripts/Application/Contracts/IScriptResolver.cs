using Aidan.Core.Patterns;
using Nexus.Scripts.Application.Requests;
using Nexus.Scripts.Application.Responses;

namespace Nexus.Scripts.Application.Contracts;

public interface IScriptResolver
{
    Task<IResult<ResolveScriptsResponse>> ResolveAsync(
        ResolveScriptsRequest request,
        CancellationToken cancellationToken = default);
}
