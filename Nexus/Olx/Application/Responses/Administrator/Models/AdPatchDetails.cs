namespace Nexus.Olx.Application.Responses.Administrator.Models;

public class AdSpoofDetails
{
    public string Id { get; init; } = string.Empty;
    public string OperationId { get; init; } = string.Empty;
    public string AdId { get; init; } = string.Empty;
    public string AdUrl { get; init; } = string.Empty;
    public string? OperatorId { get; init; }
    public bool IsImpersonating { get; init; }
    public decimal? OriginalPrice { get; init; }
    public decimal? PromotionalPrice { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
