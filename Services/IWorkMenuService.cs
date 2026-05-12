using Molina.Bedding.Mvc.Models;

namespace Molina.Bedding.Mvc.Services;

public interface IWorkMenuService
{
    IReadOnlyList<WorkAreaViewModel> GetInitialAreas();
}
