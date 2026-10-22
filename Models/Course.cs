using System;
using System.Collections.Generic;

namespace Nhom8.Models;

public partial class Course
{
    public int CourseId { get; set; }

    public string CourseTitle { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal? DiscountPrice { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string Level { get; set; } = null!;

    public string? Language { get; set; }

    public string CourseStatus { get; set; } = null!;

    public DateTime? UpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CategoryId { get; set; }

    public int UserId { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();

    public virtual User User { get; set; } = null!;
}
