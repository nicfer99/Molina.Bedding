namespace Molina.Bedding.Mvc.Models;

public class Screen4SelectedLaunchViewModel
{
    public int OrderId { get; set; }
    public string LotCode { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public decimal? QuantityToProduce { get; set; }
    public decimal? QuantityProduced { get; set; }
    public decimal? QuantityDeclared { get; set; }
    public string SelectedMaterialLotCode { get; set; } = string.Empty;
    public string ArticleCode { get; set; } = string.Empty;
    public List<string> AvailableMaterialLots { get; set; } = [];
    public string? MaterialLotValidationMessage { get; set; }
    public bool HasPreviousDeclarations { get; set; }
    public List<DeclarationHistoryItemViewModel> PreviousDeclarations { get; set; } = [];
}
