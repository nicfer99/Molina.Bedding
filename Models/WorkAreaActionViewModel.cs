namespace Molina.Bedding.Mvc.Models;

public class WorkAreaActionViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string Tone { get; init; } = "default";
    public int Row { get; init; }
    public string? HelperText { get; init; }
}
