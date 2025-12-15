using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Lessons.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int LessonId { get; set; }

        public int? ParentCommentId { get; set; }

        [MaxLength(450)]
        public string? MentionedUserId { get; set; }

        public bool IsEdited { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public bool IsReported { get; set; } = false; // واحدة بس!

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual ApplicationUser User { get; set; }
        public virtual Lesson Lesson { get; set; }
        public virtual Comment ParentComment { get; set; }
        public virtual ApplicationUser MentionedUser { get; set; }
        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
        public virtual ICollection<CommentReaction> Reactions { get; set; } = new List<CommentReaction>();
    }

    public class CommentReaction
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int CommentId { get; set; }

        [Required]
        [MaxLength(20)]
        public string ReactionType { get; set; } // like, love, haha, wow, sad, angry

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ApplicationUser User { get; set; }
        public virtual Comment Comment { get; set; }
    }

    public class CommentReply
    {
        public int Id { get; set; }

        [Required]
        public int CommentId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsEdited { get; set; } = false;

        // Navigation Properties
        public virtual Comment Comment { get; set; }
        public virtual ApplicationUser User { get; set; }
    }

    public class CommentReport
    {
        public int Id { get; set; }

        [Required]
        public int CommentId { get; set; }

        [Required]
        public string ReporterId { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; }

        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
        public bool IsResolved { get; set; } = false;

        // Navigation Properties
        public virtual Comment Comment { get; set; }
        public virtual ApplicationUser Reporter { get; set; }
    }
}