using System;
using System.Collections.Generic;

namespace Nhom8.Models;

public partial class Quiz
{
    public int QuizId { get; set; }

    public string QuizTitle { get; set; } = null!;

    public int PassScore { get; set; }

    public int SectionId { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual Section Section { get; set; } = null!;
}
