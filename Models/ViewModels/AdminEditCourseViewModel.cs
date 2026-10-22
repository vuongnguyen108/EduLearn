
using System.ComponentModel.DataAnnotations;

namespace Nhom8.ViewModels
{
    public class AdminEditCourseViewModel
    {
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khóa học.")]
        [StringLength(200, ErrorMessage = "Tên khóa học không được vượt quá 200 ký tự.")]
        public string CourseTitle { get; set; } = "";

        public string Slug { get; set; } = "";

        public string? Description { get; set; }

        [Range(typeof(decimal), "0", "999999999",
            ErrorMessage = "Giá khóa học phải lớn hơn hoặc bằng 0.")]
        public decimal Price { get; set; }

        [Range(typeof(decimal), "0", "999999999",
            ErrorMessage = "Giá ưu đãi phải lớn hơn hoặc bằng 0.")]
        public decimal? DiscountPrice { get; set; }

        public string? ThumbnailUrl { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn cấp độ.")]
        public string Level { get; set; } = "";

        public string? Language { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        public int? CategoryId { get; set; }
    }
}
