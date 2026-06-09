using Aidan.Core.Patterns;

namespace Nexus.Operators.Actors.Contracts;

public interface IUnauthenticatedUser
{
    Task<IResult<CreateAdministratorAccountResponse>> CreateAdministratorAccountAsync(CreateAdministratorAccountRequest request);
    Task<IResult<CreateOperatorAccountResponse>> CreateOperatorAccountAsync(CreateOperatorAccountRequest request);
}

public interface IAdministrator
{
    Task<IResult<CreateOperationResponse>> CreateOperationAsync(CreateOperationRequest request);
    Task<IResult<SearchOperationsResponse>> SearchOperationsAsync(SearchOperationsRequest request);
    Task<IResult> DeleteOperationAsync();
    Task<IResult> AssignOperationAdministratorAsync();
    Task<IResult> UnassignOperationAdministratorAsync();
}

public interface IOperationAdministrator
{
    // operator management
    Task<IResult> AddOperatorAsync();
    Task<IResult> RemoveOperatorAsync();

    // per strawman, per group, manual
    Task<IResult> SetGatewaySelectionStrategyAsync();
    Task<IResult> AddStrawManAsync();
    Task<IResult> RemoveStrawManAsync();
    Task<IResult> AddGatewayAccountGroupAsync();
    Task<IResult> RemoveGatewayAccountGroupAsync();
    Task<IResult> AddGatewayAccountAsync();
    Task<IResult> RemoveGatewayAccountAsync();

    // set profit share strategy
    Task<IResult> SetProfitShareStrategyAsync();

    // team management
    Task<IResult> CreateTeamAsync();
    Task<IResult> DeleteTeamAsync();
    Task<IResult> AssignTeamLeaderAsync();
    Task<IResult> UnassignTeamLeaderAsync();
}

// A team is a contexto to set their own gateway selection strategy and profit sharing in an independent way of the operation the team belongs to
public interface ITeamLeader
{
    // operator management
    Task<IResult> AddOperatorAsync();
    Task<IResult> RemoveOperatorAsync();

    // per strawman, per group, manual
    Task<IResult> SetGatewaySelectionStrategyAsync();
    Task<IResult> AddStrawManAsync();
    Task<IResult> RemoveStrawManAsync();
    Task<IResult> AddGatewayAccountGroupAsync();
    Task<IResult> RemoveGatewayAccountGroupAsync();
    Task<IResult> AddGatewayAccountAsync();
    Task<IResult> RemoveGatewayAccountAsync();

    // set profit share strategy
    Task<IResult> SetProfitShareStrategyAsync();
}

public interface IOperator
{
    Task<IResult> CreatePixPaymentAsync();
}

public interface ITeamOperator
{
    Task<IResult> CreatePixPaymentAsync();
}

// DTOS

// common
public abstract class SearchResponse<T>
{
    public int Offset { get; set; }
    public int Limit { get; set; }
    public List<T> Items { get; set; } = new();
}

// create operator account
public class CreateOperatorAccountRequest { }
public class CreateOperatorAccountResponse { }

// create admin account
public class CreateAdministratorAccountRequest { }
public class CreateAdministratorAccountResponse { }

// create operation
public class CreateOperationRequest { }
public class CreateOperationResponse { }

public class SearchOperationsRequest { }
public class SearchOperationsResponse : SearchResponse<Operation> { }
public class Operation { }
