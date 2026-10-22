using Microsoft.AspNetCore.Mvc;
using Nhom8.Models;

namespace Nhom8.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly QlkhoaHocContext _context;

        public CategoryMenuViewComponent(QlkhoaHocContext context)
        {
            _context = context;
        }
        public IViewComponentResult Invoke()
        {
            var category = _context.Categories.ToList();
            return View(category);
        }
    }
}
