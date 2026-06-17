using Aidan.Core.Patterns;
using Nexus.OperationAdministrator.Application.Requests;
using Nexus.OperationAdministrator.Application.Responses;

namespace Nexus.OperationAdministrator.Application.Contracts;

public interface IOperationAdministratorStrawManAssignmentSearchService
{
    Task<IResult<SearchStrawMenToAssignResponse>> SearchStrawMenToAssignAsync(
        SearchStrawMenToAssignRequest request);
}
