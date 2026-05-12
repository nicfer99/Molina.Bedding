namespace Molina.Bedding.Mvc.Models;

public record OperatorSelectionViewModel
{
    public List<OperatorItemViewModel> Operators { get; init; } = [];
    public string SelectedOperatorIds { get; init; } = string.Empty;
    public string? ValidationMessage { get; init; }
}
