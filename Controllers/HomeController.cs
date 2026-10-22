using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nhom8.Models;
using System.Diagnostics;

namespace Nhom8.Controllers
{
    public class HomeController : Controller
    {
        private readonly QlkhoaHocContext _context;

        public HomeController(QlkhoaHocContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            var lstCourse = _context.Courses.ToList();
            return View(lstCourse);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
