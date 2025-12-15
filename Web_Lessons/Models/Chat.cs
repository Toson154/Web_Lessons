using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Lessons.Models
{
    public class Chat
    {
        public int Id { get; set; }

        [Required]
        public string User1Id { get; set; } // الطالب

        [Required]
        public string User2Id { get; set; } // المعلم

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastMessageAt { get; set; }

        // Navigation Properties
        public virtual ApplicationUser User1 { get; set; }
        public virtual ApplicationUser User2 { get; set; }
        public virtual ICollection<ChatMessage> Messages { get; set; }
    }

    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        public int ChatId { get; set; }

        [Required]
        public string SenderId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }

        public string? AttachmentUrl { get; set; }
        public string? AttachmentType { get; set; } // image, pdf, video, etc.

        public bool IsRead { get; set; } = false;
        public DateTime ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Chat Chat { get; set; }
        public virtual ApplicationUser Sender { get; set; }
    }
}