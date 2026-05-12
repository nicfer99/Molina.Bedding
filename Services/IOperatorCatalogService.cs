using Molina.Bedding.Mvc.Models;

namespace Molina.Bedding.Mvc.Services;

public interface IOperatorCatalogService
{
    IReadOnlyList<OperatorItemViewModel> GetAllActive();
    IReadOnlyList<OperatorItemViewModel> GetByIds(IEnumerable<int> ids);
}
