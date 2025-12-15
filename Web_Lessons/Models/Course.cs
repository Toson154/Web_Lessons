using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Lessons.Models
{
    // Models/Course.cs
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public string Level { get; set; }

        // Course Image
        public string? ThumbnailUrl { get; set; }

        public bool IsPublished { get; set; } = false;

        [Required]
        public int SubjectId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Subject? Subject { get; set; }
        public virtual ICollection<Lesson>? Lessons { get; set; }
        public virtual ICollection<Enrollment>? Enrollments { get; set; }
    }
}