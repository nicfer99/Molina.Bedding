namespace Molina.Bedding.Mvc.Models;

public class ProductionLaunchSelectionViewModel
{
    public string ActionId { get; init; } = string.Empty;
    public string ActionText { get; init; } = string.Empty;
    public string AreaTitle { get; init; } = string.Empty;
    public string LineCode { get; init; } = string.Empty;
    public string LineDisplayName { get; init; } = string.Empty;
    public string ProductionMode { get; init; } = string.Empty;
    public List<OperatorItemViewModel> SelectedOperators { get; init; } = [];
    public List<ProductionLaunchItemViewModel> Launches { get; init; } = [];
    public string SelectedOrderIds { get; init; } = string.Empty;
    public bool AutoInsertOnBarcodeEnabled { get; init; }
    public bool RequiresMaterialLotSelection { get; init; }
    public List<string> AvailableMaterialLots { get; init; } = [];
    public string PrefilledSelectionsJson { get; init; } = "[]";
    public string? ValidationMessage { get; init; }
    public string? SuccessMessage { get; init; }
}
