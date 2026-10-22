using System;
using System.Collections.Generic;

namespace Nhom8.Models;

public partial class Enrollment
{
    public int EnrollmentId { get; set; }

    public DateTime EnrolledAt { get; set; }

    public decimal ProgressPercent { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? CertificateCode { get; set; }

    public int StudentId { get; set; }

    public int CourseId { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
