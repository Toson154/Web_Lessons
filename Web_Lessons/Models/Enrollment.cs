namespace Web_Lessons.Models;
using System;
using System.ComponentModel.DataAnnotations;

// Models/Enrollment.cs
public class Enrollment
{
    public int Id { get; set; }

    [Required]
    public string StudentId { get; set; }

    [Required]
    public int CourseId { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation Properties
    public virtual ApplicationUser? Student { get; set; }
    public virtual Course? Course { get; set; }
}
