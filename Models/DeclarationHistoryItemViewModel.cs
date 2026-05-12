namespace Molina.Bedding.Mvc.Models;

public class DeclarationHistoryItemViewModel
{
    public int OrderId { get; init; }
    public int DeclarationId { get; init; }
    public DateTime DeclarationDateTime { get; init; }
    public decimal DeclaredQuantity { get; init; }
    public int WorkedMinutes { get; init; }
    public string AnomalyDescription { get; init; } = string.Empty;
    public int AnomalyMinutes { get; init; }
    public string OperatorsSummary { get; init; } = string.Empty;
}
