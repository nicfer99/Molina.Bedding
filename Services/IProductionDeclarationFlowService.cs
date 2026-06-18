using Molina.Bedding.Mvc.Models;

namespace Molina.Bedding.Mvc.Services;

public interface IProductionDeclarationFlowService
{
    bool TryGetWorkAction(string? actionId, out ProductionWorkActionDefinition actionDefinition);
    bool IsProductionLaunchAction(ProductionWorkActionDefinition actionDefinition);
    bool IsGenericDeclarationAction(ProductionWorkActionDefinition actionDefinition);
    bool RequiresMaterialLotSelection(ProductionWorkActionDefinition actionDefinition, string? productionMode);
    string? NormalizeProductionMode(string? productionMode);
    string? GetDeclarationPhaseCode(ProductionWorkActionDefinition actionDefinition, string? productionMode);
    OperatorSelectionViewModel BuildOperatorsModel(IEnumerable<int> selectedIds);
    WorkMenuViewModel BuildWorkMenuModel(IEnumerable<int> selectedIds);
    ProductionLaunchSelectionViewModel BuildLaunchesModel(BlazorProductionDeclarationState state, string actionId, string? productionMode, string? validationMessage, string? successMessage);
    IReadOnlyList<ProductionLaunchItemViewModel> ResolveLoadedLaunchesFromBarcode(IEnumerable<ProductionLaunchItemViewModel> launches, string barcodeValue);
    BarcodeLaunchResult AddLaunchFromBarcode(BlazorProductionDeclarationState state, string actionId, string? productionMode, string barcodeValue, IEnumerable<int> selectedOrderIds, IEnumerable<ProductionLaunchPrefillSelectionItem> prefillSelections);
    Screen4ViewModel BuildScreen4Model(BlazorProductionDeclarationState state, string? actionId, string? validationMessage = null, string? successMessage = null);
    DeclarationDateAuthorizationResult AuthorizeDeclarationDateEdit(BlazorProductionDeclarationState state, string? pin);
    DeclarationSubmitResult InsertDeclarations(BlazorProductionDeclarationState state, Screen4InsertPostModel postModel);
    DeclarationSubmitResult InsertDirectDeclaration(BlazorProductionDeclarationState state, ProductionLaunchDirectInsertPostModel postModel);
}

public sealed record ProductionWorkActionDefinition(
    string Id,
    string AreaTitle,
    string ActionText,
    string FlowType,
    string? LineCode);

public sealed class BarcodeLaunchResult
{
    public bool Success { get; init; }
    public string? ValidationMessage { get; init; }
    public string? SuccessMessage { get; init; }
    public int? OrderId { get; init; }
    public List<int> SelectedOrderIds { get; init; } = [];
    public List<ProductionLaunchPrefillSelectionItem> PrefillSelections { get; init; } = [];
}

public sealed class DeclarationSubmitResult
{
    public bool Success { get; init; }
    public bool ClearFlow { get; init; }
    public string? Message { get; init; }
    public Screen4ViewModel? InvalidModel { get; init; }
}
