namespace Molina.Bedding.Mvc.Models;

public class ProductionLaunchItemViewModel
{
    public int OrderId { get; init; }
    public string LotCode { get; init; } = string.Empty;
    public string DocumentNumber { get; init; } = string.Empty;
    public decimal QuantityToProduce { get; init; }
    public decimal? QuantityProduced { get; set; }
    public string LineDescription { get; init; } = string.Empty;
    public string ArticleCode { get; set; } = string.Empty;
    public string StatusCode { get; init; } = string.Empty;
    public bool IsClosed => !string.Equals(StatusCode, "I", StringComparison.OrdinalIgnoreCase);
    public List<string> AvailableMaterialLots { get; set; } = [];
    public string? MaterialLotValidationMessage { get; set; }
}
