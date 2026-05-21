namespace Molina.Bedding.Mvc.Models;

public class DeclarationNoteTypeViewModel
{
    public int Id { get; init; }
    public string Description { get; init; } = string.Empty;
    public bool RequiresAnnotationText { get; init; }
}
