// ViewModels/AddReplyViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Web_Lessons.ViewModels
{
    public class AddReplyViewModel
    {
        [Required]
        public int CommentId { get; set; }

        [Required]
        [MaxLength(500, ErrorMessage = "Reply cannot exceed 500 characters")]
        public string Content { get; set; }

        public string ReturnUrl { get; set; } = "comment";
    }
}