using System;
using System.Collections.Generic;

namespace Nhom8.Models;

public partial class Section
{
    public int SectionId { get; set; }

    public string SectionTitle { get; set; } = null!;

    public int OrderIndex { get; set; }

    public int CourseId { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
