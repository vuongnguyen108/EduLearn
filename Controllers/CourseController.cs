using Microsoft.AspNetCore.Authorization;   
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom8.Models;
namespace Nhom8.Models
{
    public class CourseController : Controller
    {
        private readonly QlkhoaHocContext _context;

        public CourseController(QlkhoaHocContext context)
        {
            _context = context;
        }
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? searchTerm)
        {
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
            return View();
        }
        public async Task<IActionResult> Filter(
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            string? level,
            string? searchTerm,
            int page = 1,
            int pageSize = 8)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 24);

            var query = _context.Courses
                .AsNoTracking()
                .AsQueryable();

            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == categoryId.Value);
            }

            // Lọc theo giá ưu đãi, nếu không có thì dùng giá gốc
            if (minPrice.HasValue)
            {
                query = query.Where(c =>
                    (c.DiscountPrice ?? c.Price) >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(c =>
                    (c.DiscountPrice ?? c.Price) <= maxPrice.Value);
            }

            // Lọc theo trình độ
            if (!string.IsNullOrWhiteSpace(level))
            {
                query = query.Where(c => c.Level == level);
            }

            var totalResults = await query.CountAsync();

            // Lấy dữ liệu theo trang
            var courses = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.CourseId,
                    c.CategoryId,
                    c.CourseTitle,
                    c.Description,
                    c.ThumbnailUrl,
                    c.Level,
                    c.Price,
                    c.DiscountPrice,
                    c.Slug
                })
                .ToListAsync();

            // Tìm kiếm theo tên khóa học
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string keyword = searchTerm.Trim();

                query = query.Where(c =>
                    c.CourseTitle.Contains(keyword));
            }

            return Json(new
            {
                courses,
                totalResults,
                currentPage = page,
                pageSize,
                totalPages = (int)Math.Ceiling(
                    totalResults / (double)pageSize)
            });
        }
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Category)
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Lessons)
                .Include(c => c.Enrollments)
                .Include(c => c.Reviews)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        public async Task<IActionResult> Learn(int id, int? lessonId)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Lessons)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            var sections = course.Sections
                .OrderBy(s => s.OrderIndex)
                .ToList();

            var lessons = sections
                .SelectMany(s => s.Lessons.OrderBy(l => l.OrderIndex))
                .ToList();

            Lesson? currentLesson = null;

            if (lessonId.HasValue)
            {
                currentLesson = lessons
                    .FirstOrDefault(l => l.LessonId == lessonId.Value);
            }

            currentLesson ??= lessons.FirstOrDefault();

            var model = new LearningPageViewModel
            {
                Course = course,
                CurrentLesson = currentLesson,
                Sections = sections,
                ProgressPercent = 0
            };

            return View(model);
        }
    }
}
