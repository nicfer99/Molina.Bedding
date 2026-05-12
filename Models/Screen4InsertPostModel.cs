using System.Globalization;

namespace Molina.Bedding.Mvc.Models;

public class Screen4InsertPostModel
{
    public string ActionId { get; set; } = string.Empty;
    public string ProductionMode { get; set; } = string.Empty;
    public string DeclarationDate { get; set; } = string.Empty;
    public string ConfirmedOverLimitOrderIds { get; set; } = string.Empty;
    public int GlobalTimingHours { get; set; }
    public int GlobalTimingMinutes { get; set; }
    public string GlobalProblemDescription { get; set; } = string.Empty;
    public int GlobalProblemHours { get; set; }
    public int GlobalProblemMinutes { get; set; }
    public List<Screen4InsertRowPostItemModel> Rows { get; set; } = [];

    public int GetTimingMinutesPerOperator()
    {
        return Math.Max(0, GlobalTimingHours) * 60 + Math.Max(0, GlobalTimingMinutes);
    }

    public int GetProblemMinutes()
    {
        return Math.Max(0, GlobalProblemHours) * 60 + Math.Max(0, GlobalProblemMinutes);
    }

    public DateTime GetDeclarationDateOrDefault(DateTime defaultValue)
    {
        if (TryParseDate(DeclarationDate, out var parsedDate))
        {
            return parsedDate.Date;
        }

        return defaultValue.Date;
    }

    public IReadOnlySet<int> GetConfirmedOverLimitOrderIds()
    {
        if (string.IsNullOrWhiteSpace(ConfirmedOverLimitOrderIds))
        {
            return new HashSet<int>();
        }

        return ConfirmedOverLimitOrderIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static value => int.TryParse(value, out var parsedValue) ? parsedValue : (int?)null)
            .Where(static value => value.HasValue)
            .Select(static value => value!.Value)
            .ToHashSet();
    }

    public IReadOnlyList<ProductionDeclarationInsertRowRequest> GetDeclaredRows()
    {
        if (Rows is null || Rows.Count == 0)
        {
            return [];
        }

        return Rows
            .Where(static row => row.OrderId > 0)
            .Select(row => new
            {
                row.OrderId,
                Quantity = ParseQuantity(row.QuantityDeclared)
            })
            .Where(static item => item.Quantity.HasValue && item.Quantity.Value > 0)
            .Select(static item => new ProductionDeclarationInsertRowRequest
            {
                OrderId = item.OrderId,
                DeclaredQuantity = item.Quantity!.Value
            })
            .ToList();
    }

    public IReadOnlyDictionary<int, decimal> GetDeclaredQuantityByOrderId()
    {
        if (Rows is null || Rows.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        return Rows
            .Where(static row => row.OrderId > 0)
            .Select(row => new
            {
                row.OrderId,
                Quantity = ParseQuantity(row.QuantityDeclared)
            })
            .Where(static item => item.Quantity.HasValue && item.Quantity.Value > 0)
            .GroupBy(static item => item.OrderId)
            .ToDictionary(static group => group.Key, static group => group.Last().Quantity!.Value);
    }

    public IReadOnlyDictionary<int, string> GetSelectedMaterialLotByOrderId()
    {
        if (Rows is null || Rows.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        return Rows
            .Where(static row => row.OrderId > 0 && !string.IsNullOrWhiteSpace(row.SelectedMaterialLotCode))
            .GroupBy(static row => row.OrderId)
            .ToDictionary(
                static group => group.Key,
                static group => group.Last().SelectedMaterialLotCode.Trim());
    }

    private static bool TryParseDate(string? rawValue, out DateTime result)
    {
        if (DateTime.TryParseExact(rawValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            return true;
        }

        if (DateTime.TryParse(rawValue, CultureInfo.GetCultureInfo("it-IT"), DateTimeStyles.None, out result))
        {
            return true;
        }

        result = default;
        return false;
    }

    private static decimal? ParseQuantity(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var normalizedValue = rawValue
            .Trim()
            .Replace(" ", string.Empty)
            .Replace(",", ".");

        if (decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantValue))
        {
            return invariantValue;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.GetCultureInfo("it-IT"), out var italianValue))
        {
            return italianValue;
        }

        return null;
    }
}

public class Screen4InsertRowPostItemModel
{
    public int OrderId { get; set; }
    public string QuantityDeclared { get; set; } = string.Empty;
    public string SelectedMaterialLotCode { get; set; } = string.Empty;
}
