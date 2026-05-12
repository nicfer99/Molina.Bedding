using System.Globalization;
using System.Text.Json;

namespace Molina.Bedding.Mvc.Models;

public class ProductionLaunchSelectionPostModel
{
    public string ActionId { get; init; } = string.Empty;
    public string SelectedOrderIds { get; init; } = string.Empty;
    public string ProductionMode { get; init; } = string.Empty;
    public bool AutoInsertOnBarcodeEnabled { get; init; }
    public bool AutoFillMaxOnBarcode { get; init; }
    public string PrefilledSelectionsJson { get; init; } = "[]";


    public IReadOnlyList<ProductionLaunchPrefillSelectionItem> GetPrefilledSelections()
    {
        if (string.IsNullOrWhiteSpace(PrefilledSelectionsJson))
        {
            return [];
        }

        try
        {
            var parsedItems = JsonSerializer.Deserialize<List<ProductionLaunchPrefillSelectionItem>>(PrefilledSelectionsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];
            return parsedItems
                .Where(static item => item.OrderId > 0)
                .Select(item =>
                {
                    item.QuantityDeclared = NormalizeQuantity(item.QuantityDeclared);
                    item.SelectedMaterialLotCode = (item.SelectedMaterialLotCode ?? string.Empty).Trim();
                    return item;
                })
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string NormalizeQuantity(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        var normalizedValue = rawValue
            .Trim()
            .Replace(" ", string.Empty)
            .Replace(",", ".");

        if (decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantValue))
        {
            return invariantValue.ToString("0.##", CultureInfo.GetCultureInfo("it-IT"));
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.GetCultureInfo("it-IT"), out var italianValue))
        {
            return italianValue.ToString("0.##", CultureInfo.GetCultureInfo("it-IT"));
        }

        return string.Empty;
    }

    public IReadOnlyList<int> GetSelectedOrderIds()
    {
        if (string.IsNullOrWhiteSpace(SelectedOrderIds))
        {
            return [];
        }

        return SelectedOrderIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static value => int.TryParse(value, out var parsedValue) ? parsedValue : (int?)null)
            .Where(static value => value.HasValue)
            .Select(static value => value!.Value)
            .Distinct()
            .OrderBy(static value => value)
            .ToList();
    }
}
