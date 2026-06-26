namespace Nexus.Olx.Application.Contracts;

public class ImpersonateAdRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
    public string AdId { get; set; } = string.Empty;
}

