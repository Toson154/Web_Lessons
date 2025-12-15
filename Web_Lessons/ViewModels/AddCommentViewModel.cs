    // ViewModels/AddCommentViewModel.cs
    using System.ComponentModel.DataAnnotations;

    namespace Web_Lessons.ViewModels
    {
        public class AddCommentViewModel
        {
            [Required]
            public int LessonId { get; set; }
            
            [Required]
            [MaxLength(2000, ErrorMessage = "Comment cannot exceed 2000 characters")]
            public string Content { get; set; }

            public string ReturnUrl { get; set; } = "lesson";
            public bool IsReported { get; set; } = false;
            public string MentionedUserId { get; set; }
        }
    }