using System;
using System.Collections.Generic;

namespace Nhom8.Models;

public partial class AnswerOption
{
    public int AnswerOptionId { get; set; }

    public string OptionContent { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public int QuestionId { get; set; }

    public virtual Question Question { get; set; } = null!;
}
