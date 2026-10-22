using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom8.Models;

namespace Nhom8.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly QlkhoaHocContext _context;
        private const string CartKey = "EduLearnCart";

        public CartController(QlkhoaHocContext context)
        {
            _context = context;
        }

        private List<int> GetCart()
        {
            var json = HttpContext.Session.GetString(CartKey);

            return string.IsNullOrEmpty(json)
                ? new List<int>()
                : JsonSerializer.Deserialize<List<int>>(json)
                    ?? new List<int>();
        }

        private void SaveCart(List<int> cart)
        {
            HttpContext.Session.SetString(
                CartKey,
                JsonSerializer.Serialize(cart));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int id)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            var cart = GetCart();

            if (!cart.Contains(id))
            {
                cart.Add(id);
                SaveCart(cart);
                TempData["CartMessage"] = "Đã thêm khóa học vào giỏ hàng.";
            }
            else
            {
                TempData["CartMessage"] = "Khóa học đã có trong giỏ hàng.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BuyNow(int id)
        {
            SaveCart(new List<int> { id });

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = GetCart();

            var courses = await _context.Courses
                .AsNoTracking()
                .Where(c => cart.Contains(c.CourseId))
                .ToListAsync();

            ViewBag.Total = courses.Sum(c => c.DiscountPrice ?? c.Price);

            return View(courses);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            cart.Remove(id);
            SaveCart(cart);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();

            if (cart.Count == 0)
            {
                TempData["CartMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction(nameof(Index));
            }

            var courses = await _context.Courses
                .AsNoTracking()
                .Where(c => cart.Contains(c.CourseId))
                .ToListAsync();

            if (courses.Count != cart.Distinct().Count())
            {
                TempData["CartMessage"] =
                    "Một số khóa học không còn tồn tại. Vui lòng kiểm tra lại giỏ hàng.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Total = courses.Sum(c => c.DiscountPrice ?? c.Price);

            return View(courses);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCheckout()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int studentId))
            {
                return Challenge();
            }

            var cart = GetCart().Distinct().ToList();

            if (cart.Count == 0)
            {
                TempData["CartMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction(nameof(Index));
            }

            var courses = await _context.Courses
                .Where(c => cart.Contains(c.CourseId))
                .ToListAsync();

            if (courses.Count != cart.Count)
            {
                TempData["CartMessage"] =
                    "Một số khóa học không còn tồn tại. Vui lòng kiểm tra lại giỏ hàng.";

                return RedirectToAction(nameof(Index));
            }

            // Không mua lại khóa học đã được ghi danh.
            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e =>
                    e.StudentId == studentId &&
                    cart.Contains(e.CourseId));

            if (alreadyEnrolled)
            {
                TempData["CartMessage"] =
                    "Giỏ hàng có khóa học bạn đã sở hữu. Vui lòng xóa khóa học đó trước khi tiếp tục.";

                return RedirectToAction(nameof(Index));
            }

            var now = DateTime.Now;

            // Tính giá ở server, không sử dụng tổng tiền từ trình duyệt.
            var order = new Order
            {
                StudentId = studentId,
                CreatedAt = now,
                PaidAt = now,
                PaymentMethod = "Simulation",
                PaymentStatus = "Paid",
                TotalAmount = courses.Sum(c => c.DiscountPrice ?? c.Price),

                OrderDetails = courses.Select(c => new OrderDetail
                {
                    CourseId = c.CourseId,
                    UnitPrice = c.DiscountPrice ?? c.Price
                }).ToList()
            };

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Orders.Add(order);

                foreach (var course in courses)
                {
                    _context.Enrollments.Add(new Enrollment
                    {
                        StudentId = studentId,
                        CourseId = course.CourseId,
                        EnrolledAt = now,
                        ProgressPercent = 0
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Chỉ xóa giỏ hàng sau khi lưu dữ liệu thành công.
                HttpContext.Session.Remove(CartKey);

                return RedirectToAction(
                    nameof(CheckoutSuccess),
                    new { id = order.OrderId });
            }
            catch
            {
                await transaction.RollbackAsync();

                // Ghi log lỗi ở đây khi hoàn thiện dự án.
                TempData["CartMessage"] =
                    "Không thể hoàn tất đơn hàng. Vui lòng thử lại.";

                return RedirectToAction(nameof(Checkout));
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckoutSuccess(int id)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int studentId))
            {
                return Challenge();
            }

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Course)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == id &&
                    o.StudentId == studentId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
