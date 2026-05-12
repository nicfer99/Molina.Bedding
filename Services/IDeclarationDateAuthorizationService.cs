namespace Molina.Bedding.Mvc.Services;

public interface IDeclarationDateAuthorizationService
{
    bool CanAnySelectedOperatorEditDate(IEnumerable<int> operatorIds);
    DeclarationDateAuthorizationResult AuthorizeForDateEdit(IEnumerable<int> operatorIds, string? pin);
}

public sealed class DeclarationDateAuthorizationResult
{
    public bool Success { get; init; }
    public int Level { get; init; }
    public string Message { get; init; } = string.Empty;
}
