using System.Globalization;
using System.Text.Json;

namespace Molina.Bedding.Mvc.Models;

public class Screen4InsertPostModel
{
    public string ActionId { get; set; } = string.Empty;
    public string ProductionMode { get; set; } = string.Empty;
    public string DeclarationDate { get; set; } = string.Empty;
    public string ConfirmedOverLimitOrderIds { get; set; } = string.Empty;
    public string ConfirmedMissingMaterialLotOrderIds { get; set; } = string.Empty;
    public int GlobalTimingHours { get; set; }
    public int GlobalTimingMinutes { get; set; }
    public int? SelectedNoteTypeId { get; set; }
    public string GlobalProblemDescription { get; set; } = string.Empty;
    public int GlobalProblemHours { get; set; }
    public int GlobalProblemMinutes { get; set; }
    public string ProblemNotesJson { get; set; } = "[]";
    public List<Screen4InsertRowPostItemModel> Rows { get; set; } = [];

    public int GetTimingMinutesPerOperator()
    {
        return Math.Max(0, GlobalTimingHours) * 60 + Math.Max(0, GlobalTimingMinutes);
    }

    public int GetProblemMinutes()
    {
        return Math.Max(0, GlobalProblemHours) * 60 + Math.Max(0, GlobalProblemMinutes);
    }

    public IReadOnlyList<Screen4ProblemNotePostItemModel> GetProblemNotes()
    {
        if (string.IsNullOrWhiteSpace(ProblemNotesJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Screen4ProblemNotePostItemModel>>(ProblemNotesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })?
                .Where(static item => item.NoteTypeId > 0)
                .Select(static item => new Screen4ProblemNotePostItemModel
                {
                    NoteTypeId = item.NoteTypeId,
                    Description = item.Description?.Trim() ?? string.Empty,
                    Hours = Math.Max(0, item.Hours),
                    Minutes = Math.Max(0, item.Minutes)
                })
                .Where(static item => item.TotalMinutes > 0 || !string.IsNullOrWhiteSpace(item.Description))
                .ToList()
                ?? [];
        }
        catch
        {
            return [];
        }
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
        return ParseOrderIdSet(ConfirmedOverLimitOrderIds);
    }

    public IReadOnlySet<int> GetConfirmedMissingMaterialLotOrderIds()
    {
        return ParseOrderIdSet(ConfirmedMissingMaterialLotOrderIds);
    }

    private static IReadOnlySet<int> ParseOrderIdSet(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return new HashSet<int>();
        }

        return rawValue
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

public class Screen4ProblemNotePostItemModel
{
    public int NoteTypeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Hours { get; set; }
    public int Minutes { get; set; }
    public int TotalMinutes => Math.Max(0, Hours) * 60 + Math.Max(0, Minutes);
}
