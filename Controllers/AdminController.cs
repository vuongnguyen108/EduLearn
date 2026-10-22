using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom8.Models;
using Nhom8.ViewModels;

namespace Nhom8.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly QlkhoaHocContext _context;

        public AdminController(QlkhoaHocContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var startMonth = new DateTime(
                now.Year, now.Month, 1).AddMonths(-5);

            var paidOrders = _context.Orders
                .AsNoTracking()
                .Where(o => o.PaymentStatus == "Paid");

            var model = new AdminDashboardViewModel
            {
                TotalStudents = await _context.Users
                    .CountAsync(u => u.Role == "Student"),

                TotalRevenue = await paidOrders
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0,

                TotalCourses = await _context.Courses.CountAsync(),

                PendingOrders = await _context.Orders
                    .CountAsync(o => o.PaymentStatus == "Pending"),

                RecentCourses = await _context.Courses
                    .AsNoTracking()
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .Select(c => new RecentCourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseTitle = c.CourseTitle,
                        Price = c.DiscountPrice ?? c.Price,
                        CourseStatus = c.CourseStatus,
                        StudentCount = c.Enrollments.Count()
                    })
                    .ToListAsync()
            };

            var revenueRows = await paidOrders
                .Where(o => o.PaidAt.HasValue
                    && o.PaidAt.Value >= startMonth)
                .GroupBy(o => new
                {
                    o.PaidAt!.Value.Year,
                    o.PaidAt!.Value.Month
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            for (int i = 0; i < 6; i++)
            {
                var month = startMonth.AddMonths(i);

                var revenue = revenueRows
                    .Where(r => r.Year == month.Year
                        && r.Month == month.Month)
                    .Select(r => r.Revenue)
                    .FirstOrDefault();

                model.RevenueData.Add(new RevenuePointViewModel
                {
                    Label = month.ToString("MM/yyyy"),
                    Revenue = revenue
                });
            }

            return View(model);
        }


        public async Task<IActionResult> Courses(string? searchTerm, string? status)
        {
            var query = _context.Courses
                .AsNoTracking()
                .Include(c => c.Category)
                .Include(c => c.Enrollments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim();

                query = query.Where(c =>
                    c.CourseTitle.Contains(keyword) ||
                    c.Slug.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.CourseStatus == status);
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedStatus = status;

            ViewBag.Statuses = await _context.Courses
                .AsNoTracking()
                .Select(c => c.CourseStatus)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            var courses = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(courses);
        }

        public async Task<IActionResult> Orders(string? searchTerm, string? status)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Student)
                .Include(o => o.OrderDetails)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim();

                if (int.TryParse(keyword, out int orderId))
                {
                    query = query.Where(o =>
                        o.OrderId == orderId ||
                        o.Student.FullName.Contains(keyword) ||
                        o.Student.Email.Contains(keyword));
                }
                else
                {
                    query = query.Where(o =>
                        o.Student.FullName.Contains(keyword) ||
                        o.Student.Email.Contains(keyword));
                }
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.PaymentStatus == status);
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedStatus = status;

            ViewBag.Statuses = await _context.Orders
                .AsNoTracking()
                .Select(o => o.PaymentStatus)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }


        private async Task LoadCategoriesAsync()
        {
            ViewBag.Categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                })
                .ToListAsync();
        }

        private static string GenerateSlug(string title)
        {
            string normalized = title.Trim().ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder();

            foreach (char ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch)
                    != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(ch);
                }
            }

            string slug = builder.ToString()
                .Normalize(NormalizationForm.FormC);

            slug = Regex.Replace(slug, @"[^a-z0-9]+", "-");
            return slug.Trim('-');
        }

        [HttpGet]
        public async Task<IActionResult> CreateCourse()
        {
            await LoadCategoriesAsync();

            return View(new AdminCreateCourseViewModel
            {
                Level = "Beginner"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(
            AdminCreateCourseViewModel model)
        {
            if (model.Price < 0)
            {
                ModelState.AddModelError(
                    nameof(model.Price), "Giá không được âm.");
            }

            if (model.DiscountPrice.HasValue &&
                (model.DiscountPrice.Value < 0 ||
                 model.DiscountPrice.Value > model.Price))
            {
                ModelState.AddModelError(
                    nameof(model.DiscountPrice),
                    "Giá ưu đãi phải từ 0 đến giá gốc.");
            }

            bool categoryExists = model.CategoryId.HasValue &&
                await _context.Categories.AnyAsync(
                    c => c.CategoryId == model.CategoryId.Value);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Danh mục được chọn không tồn tại.");
            }

            int userId = 0;
            bool validUserId = int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId);

            if (!validUserId ||
                !await _context.Users.AnyAsync(u => u.UserId == userId))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Không xác định được tài khoản hiện tại.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return View(model);
            }

            string baseSlug = GenerateSlug(model.CourseTitle);

            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                ModelState.AddModelError(
                    nameof(model.CourseTitle),
                    "Tên khóa học không tạo được Slug hợp lệ.");

                await LoadCategoriesAsync();
                return View(model);
            }

            string slug = baseSlug;
            int suffix = 2;

            while (await _context.Courses.AnyAsync(c => c.Slug == slug))
            {
                slug = $"{baseSlug}-{suffix}";
                suffix++;
            }

            var course = new Course
            {
                CourseTitle = model.CourseTitle.Trim(),
                Slug = slug,
                Description = model.Description,
                Price = model.Price,
                DiscountPrice = model.DiscountPrice,
                ThumbnailUrl = string.IsNullOrWhiteSpace(model.ThumbnailUrl)
                    ? null
                    : model.ThumbnailUrl.Trim(),
                Level = model.Level,
                Language = model.Language,
                CourseStatus = "Draft",
                CreatedAt = DateTime.Now,
                CategoryId = model.CategoryId!.Value,
                UserId = userId
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thêm khóa học thành công.";

            return RedirectToAction(nameof(Courses));
        }



        [HttpGet]
        public async Task<IActionResult> EditCourse(int id)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            await LoadCategoriesAsync();

            var model = new AdminEditCourseViewModel
            {
                CourseId = course.CourseId,
                CourseTitle = course.CourseTitle,
                Slug = course.Slug,
                Description = course.Description,
                Price = course.Price,
                DiscountPrice = course.DiscountPrice,
                ThumbnailUrl = course.ThumbnailUrl,
                Level = course.Level,
                Language = course.Language,
                CategoryId = course.CategoryId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(
            int id, AdminEditCourseViewModel model)
        {
            if (id != model.CourseId)
            {
                return BadRequest();
            }

            if (model.DiscountPrice.HasValue &&
                (model.DiscountPrice.Value < 0 ||
                 model.DiscountPrice.Value > model.Price))
            {
                ModelState.AddModelError(
                    nameof(model.DiscountPrice),
                    "Giá ưu đãi phải từ 0 đến giá gốc.");
            }

            bool categoryExists = model.CategoryId.HasValue &&
                await _context.Categories.AnyAsync(
                    c => c.CategoryId == model.CategoryId.Value);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Danh mục được chọn không tồn tại.");
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return View(model);
            }

            course.CourseTitle = model.CourseTitle.Trim();
            course.Description = model.Description;
            course.Price = model.Price;
            course.DiscountPrice = model.DiscountPrice;
            course.ThumbnailUrl = string.IsNullOrWhiteSpace(model.ThumbnailUrl)
                ? null
                : model.ThumbnailUrl.Trim();
            course.Level = model.Level;
            course.Language = model.Language;
            course.CategoryId = model.CategoryId!.Value;
            course.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật khóa học thành công.";

            return RedirectToAction(nameof(Courses));
        }



        [HttpGet]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Category)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            ViewBag.HasEnrollments = await _context.Enrollments
                .AnyAsync(e => e.CourseId == id);

            ViewBag.HasOrderDetails = await _context.OrderDetails
                .AnyAsync(d => d.CourseId == id);

            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseConfirmed(int id)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            bool hasEnrollments = await _context.Enrollments
                .AnyAsync(e => e.CourseId == id);

            bool hasOrderDetails = await _context.OrderDetails
                .AnyAsync(d => d.CourseId == id);

            if (hasEnrollments || hasOrderDetails)
            {
                TempData["ErrorMessage"] =
                    "Không thể xóa khóa học đã có học viên đăng ký hoặc đã xuất hiện trong đơn hàng.";

                return RedirectToAction(nameof(Courses));
            }

            // Không xóa tự động các Section, Lesson, Quiz hoặc Review.
            // Kiểm tra các quan hệ này trước khi quyết định xóa.
            bool hasSections = await _context.Sections
                .AnyAsync(s => s.CourseId == id);

            bool hasReviews = await _context.Reviews
                .AnyAsync(r => r.CourseId == id);

            if (hasSections || hasReviews)
            {
                TempData["ErrorMessage"] =
                    "Khóa học vẫn có chương học hoặc đánh giá liên quan. " +
                    "Không thể xóa an toàn bằng thao tác này.";

                return RedirectToAction(nameof(Courses));
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa khóa học thành công.";

            return RedirectToAction(nameof(Courses));
        }

    }
}
