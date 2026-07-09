using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Scripts.Application.Contracts;
using Nexus.Scripts.Application.Requests;
using Nexus.Scripts.Application.Responses;

namespace Nexus.Scripts.Application.Contracts;

public interface IScriptAdministrator
{
    Task<IOperationResult<CreateScriptResponse>> CreateScriptAsync(
        RequesterIdentity identity,
        CreateScriptRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<SearchScriptsResponse>> SearchScriptsAsync(
        RequesterIdentity identity,
        SearchScriptsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<PublishReleaseResponse>> PublishReleaseAsync(
        RequesterIdentity identity,
        string scriptId,
        PublishReleaseRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<bool>> PromoteReleaseAsync(
        RequesterIdentity identity,
        string scriptId,
        string channelRouteValue,
        PromoteReleaseRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<bool>> AddCustomChannelAsync(
        RequesterIdentity identity,
        string scriptId,
        AddCustomChannelRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<bool>> DeprecateReleaseAsync(
        RequesterIdentity identity,
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<bool>> RestoreReleaseAsync(
        RequesterIdentity identity,
        string scriptId,
        string releaseId,
        CancellationToken cancellationToken = default);
}
