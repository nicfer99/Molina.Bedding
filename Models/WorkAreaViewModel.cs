namespace Molina.Bedding.Mvc.Models;

public class WorkAreaViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public List<WorkAreaActionViewModel> Actions { get; init; } = [];
}
