using Aidan.Core.Patterns;
using Nexus.OperationAdministrators.Application.Requests;
using Nexus.OperationAdministrators.Application.Responses;

namespace Nexus.OperationAdministrators.Application.Contracts;

public interface IOperationAdministratorStrawManAssignmentSearchService
{
    Task<IResult<SearchStrawMenToAssignResponse>> SearchStrawMenToAssignAsync(
        SearchStrawMenToAssignRequest request);
}
