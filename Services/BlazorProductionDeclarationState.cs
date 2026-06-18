using System.Text.Json;
using Microsoft.JSInterop;
using Molina.Bedding.Mvc.Models;

namespace Molina.Bedding.Mvc.Services;

public sealed class BlazorProductionDeclarationState
{
    private const string StorageKey = "molina.bedding.blazor.productionDeclaration";
    private readonly IJSRuntime _jsRuntime;
    private bool _loaded;

    public BlazorProductionDeclarationState(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public List<int> SelectedOperatorIds { get; private set; } = [];
    public string SelectedActionId { get; private set; } = string.Empty;
    public List<int> SelectedLaunchOrderIds { get; private set; } = [];
    public string ProductionMode { get; private set; } = string.Empty;
    public bool AutoFillMaxQuantityFromBarcode { get; private set; }
    public bool AutoInsertOnBarcode { get; private set; }
    public List<ProductionLaunchPrefillSelectionItem> LaunchPrefillSelections { get; private set; } = [];
    public bool DateEditAuthorized { get; private set; }
    public string? StartSuccessMessage { get; private set; }

    public async ValueTask EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        try
        {
            var rawValue = await _jsRuntime.InvokeAsync<string?>("molinaBlazorStorage.get", StorageKey);
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return;
            }

            var snapshot = JsonSerializer.Deserialize<StateSnapshot>(rawValue, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (snapshot is null)
            {
                return;
            }

            SelectedOperatorIds = NormalizeIds(snapshot.SelectedOperatorIds);
            SelectedActionId = snapshot.SelectedActionId ?? string.Empty;
            SelectedLaunchOrderIds = NormalizeIds(snapshot.SelectedLaunchOrderIds);
            ProductionMode = snapshot.ProductionMode ?? string.Empty;
            AutoFillMaxQuantityFromBarcode = snapshot.AutoFillMaxQuantityFromBarcode;
            AutoInsertOnBarcode = snapshot.AutoInsertOnBarcode;
            LaunchPrefillSelections = snapshot.LaunchPrefillSelections?.Where(static item => item.OrderId > 0).ToList() ?? [];
            DateEditAuthorized = snapshot.DateEditAuthorized;
            StartSuccessMessage = snapshot.StartSuccessMessage;
        }
        catch
        {
            ResetInMemory();
        }
    }

    public async ValueTask SaveAsync()
    {
        var snapshot = new StateSnapshot
        {
            SelectedOperatorIds = SelectedOperatorIds,
            SelectedActionId = SelectedActionId,
            SelectedLaunchOrderIds = SelectedLaunchOrderIds,
            ProductionMode = ProductionMode,
            AutoFillMaxQuantityFromBarcode = AutoFillMaxQuantityFromBarcode,
            AutoInsertOnBarcode = AutoInsertOnBarcode,
            LaunchPrefillSelections = LaunchPrefillSelections,
            DateEditAuthorized = DateEditAuthorized,
            StartSuccessMessage = StartSuccessMessage
        };

        await _jsRuntime.InvokeVoidAsync("molinaBlazorStorage.set", StorageKey, JsonSerializer.Serialize(snapshot));
    }

    public async ValueTask ClearAsync()
    {
        ResetInMemory();
        _loaded = true;
        await _jsRuntime.InvokeVoidAsync("molinaBlazorStorage.remove", StorageKey);
    }

    public async ValueTask SetOperatorsAsync(IEnumerable<int> operatorIds)
    {
        SelectedOperatorIds = NormalizeIds(operatorIds);
        SelectedActionId = string.Empty;
        SelectedLaunchOrderIds = [];
        ProductionMode = string.Empty;
        AutoFillMaxQuantityFromBarcode = false;
        AutoInsertOnBarcode = false;
        LaunchPrefillSelections = [];
        DateEditAuthorized = false;
        StartSuccessMessage = null;
        await SaveAsync();
    }

    public async ValueTask SetActionAsync(string actionId, string? productionMode)
    {
        SelectedActionId = actionId;
        ProductionMode = productionMode ?? string.Empty;
        SelectedLaunchOrderIds = [];
        AutoFillMaxQuantityFromBarcode = false;
        LaunchPrefillSelections = [];
        DateEditAuthorized = false;
        StartSuccessMessage = null;
        await SaveAsync();
    }

    public async ValueTask SetLaunchSelectionAsync(
        string actionId,
        string? productionMode,
        IEnumerable<int> orderIds,
        bool autoInsertOnBarcode,
        bool autoFillMaxQuantityFromBarcode,
        IEnumerable<ProductionLaunchPrefillSelectionItem> prefillSelections)
    {
        SelectedActionId = actionId;
        ProductionMode = productionMode ?? string.Empty;
        SelectedLaunchOrderIds = NormalizeIds(orderIds);
        AutoInsertOnBarcode = autoInsertOnBarcode;
        AutoFillMaxQuantityFromBarcode = autoFillMaxQuantityFromBarcode;
        LaunchPrefillSelections = NormalizePrefillSelections(prefillSelections);
        StartSuccessMessage = null;
        await SaveAsync();
    }

    public async ValueTask SetAutoInsertAsync(bool enabled)
    {
        AutoInsertOnBarcode = enabled;
        await SaveAsync();
    }

    public async ValueTask SetDateEditAuthorizedAsync(bool authorized)
    {
        DateEditAuthorized = authorized;
        await SaveAsync();
    }

    public async ValueTask ConsumeAutoFillMaxQuantityAsync()
    {
        if (!AutoFillMaxQuantityFromBarcode)
        {
            return;
        }

        AutoFillMaxQuantityFromBarcode = false;
        await SaveAsync();
    }

    public async ValueTask CompleteFlowAsync(string successMessage)
    {
        ResetInMemory();
        StartSuccessMessage = successMessage;
        _loaded = true;
        await SaveAsync();
    }

    public async ValueTask ConsumeStartSuccessMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(StartSuccessMessage))
        {
            return;
        }

        StartSuccessMessage = null;
        await SaveAsync();
    }

    private void ResetInMemory()
    {
        SelectedOperatorIds = [];
        SelectedActionId = string.Empty;
        SelectedLaunchOrderIds = [];
        ProductionMode = string.Empty;
        AutoFillMaxQuantityFromBarcode = false;
        AutoInsertOnBarcode = false;
        LaunchPrefillSelections = [];
        DateEditAuthorized = false;
        StartSuccessMessage = null;
    }

    private static List<int> NormalizeIds(IEnumerable<int>? ids)
    {
        return ids?
            .Where(static value => value > 0)
            .Distinct()
            .OrderBy(static value => value)
            .ToList()
            ?? [];
    }

    private static List<ProductionLaunchPrefillSelectionItem> NormalizePrefillSelections(IEnumerable<ProductionLaunchPrefillSelectionItem>? items)
    {
        return items?
            .Where(static item => item.OrderId > 0)
            .GroupBy(static item => item.OrderId)
            .Select(static group => group.Last())
            .Select(static item => new ProductionLaunchPrefillSelectionItem
            {
                OrderId = item.OrderId,
                QuantityDeclared = item.QuantityDeclared ?? string.Empty,
                SelectedMaterialLotCode = item.SelectedMaterialLotCode?.Trim() ?? string.Empty
            })
            .ToList()
            ?? [];
    }

    private sealed class StateSnapshot
    {
        public List<int> SelectedOperatorIds { get; set; } = [];
        public string? SelectedActionId { get; set; }
        public List<int> SelectedLaunchOrderIds { get; set; } = [];
        public string? ProductionMode { get; set; }
        public bool AutoFillMaxQuantityFromBarcode { get; set; }
        public bool AutoInsertOnBarcode { get; set; }
        public List<ProductionLaunchPrefillSelectionItem> LaunchPrefillSelections { get; set; } = [];
        public bool DateEditAuthorized { get; set; }
        public string? StartSuccessMessage { get; set; }
    }
}
