using System.Globalization;

namespace Molina.Bedding.Mvc.Models;

public class ProductionLaunchDirectInsertPostModel
{
    public string ActionId { get; set; } = string.Empty;
    public string ProductionMode { get; set; } = string.Empty;
    public bool AutoInsertOnBarcodeEnabled { get; set; }
    public int OrderId { get; set; }
    public string QuantityDeclared { get; set; } = string.Empty;
    public string SelectedMaterialLotCode { get; set; } = string.Empty;
    public int TimingHours { get; set; }
    public int TimingMinutes { get; set; }
    public bool ConfirmOverLimit { get; set; }

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

    public int GetTimingMinutesPerOperator()
    {
        return Math.Max(0, TimingHours) * 60 + Math.Max(0, TimingMinutes);
    }
}
