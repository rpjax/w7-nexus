namespace Nexus.Olx.Application.Requests;

public class UpdateAdDetailsSpoofRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string AdId { get; set; } = string.Empty;
    public decimal? OriginalPrice { get; set; }
    public decimal? PromotionalPrice { get; set; }
}

