namespace Molina.Bedding.Mvc.Models;

public class Screen4ViewModel
{
    public string ActionId { get; set; } = string.Empty;
    public string ActionText { get; set; } = string.Empty;
    public string AreaTitle { get; set; } = string.Empty;
    public string? LineCode { get; set; }
    public string? LineDisplayName { get; set; }
    public string ProductionMode { get; set; } = string.Empty;
    public List<OperatorItemViewModel> SelectedOperators { get; set; } = [];
    public List<Screen4SelectedLaunchViewModel> SelectedLaunches { get; set; } = [];
    public string BackActionName { get; set; } = "WorkMenu";
    public string? ValidationMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public bool IsTimingOnlyMode { get; set; }
    public bool RequiresMaterialLotSelection { get; set; }
    public bool AutoFillMaxQuantityFromBarcode { get; set; }
    public List<string> AvailableMaterialLots { get; set; } = [];
    public decimal TotalDeclared { get; set; }
    public int GlobalTimingHours { get; set; }
    public int GlobalTimingMinutes { get; set; }
    public string GlobalProblemDescription { get; set; } = string.Empty;
    public int GlobalProblemHours { get; set; }
    public int GlobalProblemMinutes { get; set; }
    public DateTime DeclarationDate { get; set; } = DateTime.Today;
    public bool CanRequestDateEdit { get; set; }
    public bool IsDeclarationDateEditable { get; set; }
    public string ConfirmedOverLimitOrderIds { get; set; } = string.Empty;
}
