namespace Nexus.Charges.Application.Models;

public sealed class ResolveCredentialsRequest
{
    public string OperationId { get; init; } = string.Empty;
    public string? OperatorId { get; init; }
}
