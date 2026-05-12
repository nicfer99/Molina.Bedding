using Molina.Bedding.Mvc.Models;

namespace Molina.Bedding.Mvc.Services;

public interface IProductionLaunchService
{
    IReadOnlyList<ProductionLaunchItemViewModel> GetOpenLaunches(string lineCode, bool resolveMaterialLots = false);
    IReadOnlyList<ProductionLaunchItemViewModel> GetOpenLaunchesByOrderIds(string lineCode, IReadOnlyList<int> orderIds, bool resolveMaterialLots = false);
}
