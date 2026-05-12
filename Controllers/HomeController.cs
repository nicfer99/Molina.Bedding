using Microsoft.AspNetCore.Mvc;

namespace Molina.Bedding.Mvc.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Start", "ProductionDeclaration");
    }

    public IActionResult Error()
    {
        return View();
    }
}
