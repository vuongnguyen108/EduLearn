using System.Collections.Generic;
namespace Nhom8.Models
{
    public class LearningPageViewModel
    {
        public Course Course { get; set; } = null!;

        public Lesson? CurrentLesson { get; set; } = null!;

        public decimal ProgressPercent { get; set; }

        public List<Section> Sections { get; set; } = new();
    }
}
