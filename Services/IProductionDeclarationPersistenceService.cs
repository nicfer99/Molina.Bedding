using Molina.Bedding.Mvc.Models;

namespace Molina.Bedding.Mvc.Services;

public interface IProductionDeclarationPersistenceService
{
    IReadOnlyList<DeclarationHistoryItemViewModel> GetPreviousDeclarationsByOrderIds(string lineCode, string? phaseCode, IReadOnlyList<int> orderIds);
    int InsertDeclaration(ProductionDeclarationInsertRequest request);
}
