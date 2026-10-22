namespace Nhom8.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }

        public decimal TotalRevenue { get; set; }

        public int TotalCourses { get; set; }

        public int PendingOrders { get; set; }

        public List<RecentCourseViewModel> RecentCourses { get; set; } = new();

        public List<RevenuePointViewModel> RevenueData { get; set; } = new();
    }

    public class RecentCourseViewModel
    {
        public int CourseId { get; set; }

        public string CourseTitle { get; set; } = "";

        public decimal Price { get; set; }

        public string CourseStatus { get; set; } = "";

        public int StudentCount { get; set; }
    }

    public class RevenuePointViewModel
    {
        public string Label { get; set; } = "";

        public decimal Revenue { get; set; }
    }
}
