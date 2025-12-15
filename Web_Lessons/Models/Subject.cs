    namespace Web_Lessons.Models;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    // Models/Subject.cs
    public class Subject
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // Subject Image
        public string? ImageUrl { get; set; }

        [Required]
        public string TeacherId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual ApplicationUser? Teacher { get; set; }
        public virtual ICollection<Course>? Courses { get; set; }
    }
