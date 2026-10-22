using System;
using System.Collections.Generic;

namespace Nhom8.Models;

public partial class Lesson
{
    public int LessonId { get; set; }

    public string LessonTitle { get; set; } = null!;

    public string? VideoUrl { get; set; }

    public string? Content { get; set; }

    public int? DurationSeconds { get; set; }

    public int OrderIndex { get; set; }

    public bool IsPreview { get; set; }

    public int SectionId { get; set; }

    public virtual Section Section { get; set; } = null!;
}
