using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Lessons.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } // mention, reaction, reply, etc.

        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string Message { get; set; }

        public int? RelatedId { get; set; }

        [MaxLength(50)]
        public string RelatedType { get; set; } // comment, lesson, course

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}