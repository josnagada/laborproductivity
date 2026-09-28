// Mengimpor fitur Controller dan IActionResult
using Microsoft.AspNetCore.Mvc;

namespace LaborProductivity.Controllers
{
    // Controller untuk menangani user yang tidak memiliki izin akses
    public class AccessDeniedController : Controller
    {
        // Menampilkan halaman Access Denied
        public IActionResult Index()
        {
            return View();
        }
    }
}