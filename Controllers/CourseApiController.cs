using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nhom8.Models;

namespace Nhom8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourseApiController : ControllerBase
    {
        private readonly QlkhoaHocContext _context;

        public CourseApiController(QlkhoaHocContext context)
        {
            _context = context;
        }
        [HttpGet()]
        public IEnumerable<Course> getAllCourses()
        {
            var allCourses = _context.Courses.ToList();
            return allCourses;
        }
        [HttpGet("{categoryId}")]
        public IEnumerable<Course> getCourseByCategoryId(int categoryId)
        {
            var courses = _context.Courses
                .Where(x => x.CategoryId == categoryId)
                .Take(8)
                .ToList();
            return courses;
        }
    }
}
