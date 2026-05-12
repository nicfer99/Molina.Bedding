namespace Molina.Bedding.Mvc.Models;

public class OperatorSelectionPostModel
{
    public string SelectedOperatorIds { get; set; } = string.Empty;

    public IEnumerable<int> GetSelectedIds()
    {
        if (string.IsNullOrWhiteSpace(SelectedOperatorIds))
        {
            return Enumerable.Empty<int>();
        }

        return SelectedOperatorIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static value => int.TryParse(value, out var parsedValue) ? parsedValue : (int?)null)
            .Where(static value => value.HasValue)
            .Select(static value => value!.Value)
            .Distinct()
            .OrderBy(static value => value);
    }
}
