using System;
using System.ComponentModel.DataAnnotations;

namespace Web_Lessons.Models
{
    // Models/LessonProgress.cs
    public class LessonProgress
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; }

        [Required]
        public int LessonId { get; set; }

        public bool IsCompleted { get; set; } = false;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? TimeSpentMinutes { get; set; }

        // Navigation Properties
        public virtual ApplicationUser? Student { get; set; }
        public virtual Lesson? Lesson { get; set; }
    }
}