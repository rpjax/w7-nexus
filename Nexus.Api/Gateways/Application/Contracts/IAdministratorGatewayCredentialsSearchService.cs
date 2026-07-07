using Aidan.Core.Patterns;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Application.Requests.Administrator;
using Nexus.Gateways.Application.Responses.Administrator;

namespace Nexus.Gateways.Application.Contracts;

public interface IAdministratorGatewayCredentialsSearchService
{
    Task<IResult<SearchGatewayCredentialsResponse>> SearchCredentialsAsync(
        PaymentGateway provider,
        SearchGatewayCredentialsRequest? request);
}
