namespace Nexus.Olx.Application.Requests;

public class UpdateAdDetailsPatchRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string AdId { get; set; } = string.Empty;
    public decimal? OriginalPrice { get; set; }
    public decimal? PromotionalPrice { get; set; }
}

