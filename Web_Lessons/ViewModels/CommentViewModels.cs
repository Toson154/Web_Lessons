using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Web_Lessons.ViewModels
{

    public class CreateCommentViewModel
    {
        [Required]
        public int LessonId { get; set; }

        [Required]
        [MaxLength(2000, ErrorMessage = "Comment cannot exceed 2000 characters")]
        public string Content { get; set; }

        public int? ParentCommentId { get; set; }
        public string? MentionedUserId { get; set; }
    }
    public class EditCommentViewModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }

        public string MentionedUserId { get; set; }

        // For StudentController
        public int LessonId { get; set; } // Add this
        public string LessonTitle { get; set; }

        // For CommentsController (rename to avoid conflict)
        [Required]
        public int CommentId { get; set; }
    }
    public class CommentViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserProfileImage { get; set; }
        public bool IsTeacher { get; set; }
        public int? ParentCommentId { get; set; }
        public string MentionedUserId { get; set; }
        public string MentionedUserName { get; set; }
        public bool IsEdited { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // أضف هذه الخواص
        public string TimeAgo { get; set; } // أضف هذا السطر
        public List<CommentViewModel> Replies { get; set; } = new List<CommentViewModel>();
        public int RepliesCount { get; set; }
        public Dictionary<string, int> Reactions { get; set; } = new Dictionary<string, int>();
        public string CurrentUserReaction { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public int LessonId { get; set; }
        public string LessonTitle { get; set; }
    }

    public class CommentReactionViewModel
    {
        public string ReactionType { get; set; }
        public int Count { get; set; }
        public bool IsCurrentUserReacted { get; set; }
    }
    public class CommentsResponseViewModel
    {
        public List<CommentViewModel> Comments { get; set; } = new List<CommentViewModel>();
        public int TotalComments { get; set; }
        public bool CanComment { get; set; }
        public string CurrentUserId { get; set; }
    }

}