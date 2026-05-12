namespace Molina.Bedding.Mvc.Configuration;

public class DeclarationDateAuthorizationEntry
{
    public int OperatorId { get; set; }
    public int Level { get; set; }
    public string Pin { get; set; } = string.Empty;
}
