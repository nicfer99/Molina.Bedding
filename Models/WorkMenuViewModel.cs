namespace Molina.Bedding.Mvc.Models;

public class WorkMenuViewModel
{
    public List<OperatorItemViewModel> SelectedOperators { get; init; } = [];
    public List<WorkAreaViewModel> Areas { get; init; } = [];
}
