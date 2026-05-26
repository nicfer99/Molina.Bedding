namespace Molina.Bedding.Mvc.Models;

public class ProductionDeclarationInsertRequest
{
    public string LineCode { get; init; } = string.Empty;
    public string? PhaseCode { get; init; }
    public DateTime DeclarationDate { get; init; } = DateTime.Today;
    public int TimingMinutes { get; init; }
    public int HeaderTimingMinutes { get; init; }
    public int? NoteTypeId { get; init; }
    public int NoteMinutes { get; init; }
    public string? NoteDescription { get; init; }
    public string? AnomalyDescription { get; init; }
    public int AnomalyMinutes { get; init; }
    public IReadOnlyList<ProductionDeclarationNoteRequest> Notes { get; init; } = [];
    public IReadOnlyList<int> OperatorIds { get; init; } = [];
    public IReadOnlyList<ProductionDeclarationInsertRowRequest> Rows { get; init; } = [];
}

public class ProductionDeclarationNoteRequest
{
    public int NoteTypeId { get; init; }
    public int Minutes { get; init; }
    public string? Description { get; init; }
}

public class ProductionDeclarationInsertRowRequest
{
    public int OrderId { get; init; }
    public decimal DeclaredQuantity { get; init; }
    public string ArticleCode { get; init; } = string.Empty;
    public string SelectedMaterialLotCode { get; init; } = string.Empty;
}
