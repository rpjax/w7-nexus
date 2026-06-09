using Aidan.Core.Patterns;
using Nexus.Operators.Actors.Requests;
using Nexus.Operators.Actors.Responses;

namespace Nexus.Operators.Actors.Contracts;

public interface IOperator
{
    Task<IResult<CreateOperationPixPaymentResponse>> CreateOperationPixPaymentAsync(
        CreateOperationPixPaymentRequest request);
}
