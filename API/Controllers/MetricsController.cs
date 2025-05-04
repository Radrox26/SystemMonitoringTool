using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class MetricsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
