using System;
using System.Collections.Generic;

namespace Nhom8.Models;

public partial class Question
{
    public int QuestionId { get; set; }

    public string QuestionContent { get; set; } = null!;

    public string QuestionType { get; set; } = null!;

    public int QuizId { get; set; }

    public virtual ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();

    public virtual Quiz Quiz { get; set; } = null!;
}
