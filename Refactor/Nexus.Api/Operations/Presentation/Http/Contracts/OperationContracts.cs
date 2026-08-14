namespace Refactor.Nexus.Api.Operations.Presentation.Http.Contracts;

public sealed class CreateOperationRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal? ManagementCutPercent { get; set; }
}

public sealed class TransitionOperationRequest
{
    public string TargetStatus { get; set; } = string.Empty;
}

public sealed class ConfigureCutRequest
{
    public decimal? ManagementCutPercent { get; set; }
}

public sealed class AssignOperatorRequest
{
    public string MemberId { get; set; } = string.Empty;
}

public sealed class RegisterScriptRequest
{
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public sealed class UpsertStoreObjectRequest
{
    public string? ObjectId { get; set; }
    public string ObjectType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
}
