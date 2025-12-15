using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Lessons.Models
{
    // Models/Lesson.cs
    public class Lesson
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public string? VideoUrl { get; set; }
        public string? PdfUrl { get; set; }

        public int Order { get; set; }
        public int DurationMinutes { get; set; }

        [Required]
        public int CourseId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Course? Course { get; set; }
        public virtual ICollection<LessonProgress>? LessonProgresses { get; set; }
        public virtual ICollection<Comment>? Comments { get; set; }
    }
}