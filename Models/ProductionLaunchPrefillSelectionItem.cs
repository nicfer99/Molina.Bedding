using System.Globalization;

namespace Molina.Bedding.Mvc.Models;

public class ProductionLaunchPrefillSelectionItem
{
    public int OrderId { get; set; }
    public string QuantityDeclared { get; set; } = string.Empty;
    public string SelectedMaterialLotCode { get; set; } = string.Empty;

    public decimal? GetDeclaredQuantity()
    {
        if (string.IsNullOrWhiteSpace(QuantityDeclared))
        {
            return null;
        }

        var normalizedValue = QuantityDeclared
            .Trim()
            .Replace(" ", string.Empty)
            .Replace(",", ".");

        if (decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantValue))
        {
            return invariantValue;
        }

        if (decimal.TryParse(QuantityDeclared, NumberStyles.Number, CultureInfo.GetCultureInfo("it-IT"), out var italianValue))
        {
            return italianValue;
        }

        return null;
    }
}
