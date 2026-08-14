namespace Refactor.Nexus.Api.Operations.Domain.Errors;

public static class OperationErrorCodes
{
    public const string RequestBodyRequired = "Operation.REQUEST_BODY_REQUIRED";
    public const string NotFound = "Operation.NOT_FOUND";
    public const string NameEmpty = "Operation.NAME_EMPTY";
    public const string InvalidTransition = "Operation.INVALID_TRANSITION";
    public const string AlreadyClosed = "Operation.ALREADY_CLOSED";
    public const string CutInvalid = "Operation.CUT_INVALID";
    public const string Unauthorized = "Operation.UNAUTHORIZED";
    public const string OperatorNotEligible = "Operation.OPERATOR_NOT_ELIGIBLE";
    public const string AlreadyAssigned = "Operation.ALREADY_ASSIGNED";
    public const string NotAssigned = "Operation.NOT_ASSIGNED";
    public const string TipManagementConflict = "Operation.TIP_MANAGEMENT_CONFLICT";
    public const string ScriptNotFound = "Operation.SCRIPT_NOT_FOUND";
    public const string ScriptResolveBlocked = "Operation.SCRIPT_RESOLVE_BLOCKED";
    public const string StoreObjectNotFound = "Operation.STORE_OBJECT_NOT_FOUND";
    public const string StoreWriteBlocked = "Operation.STORE_WRITE_BLOCKED";
    public const string StoreKeyMismatch = "Operation.STORE_KEY_MISMATCH";
    public const string KeyEmpty = "Operation.KEY_EMPTY";
}
