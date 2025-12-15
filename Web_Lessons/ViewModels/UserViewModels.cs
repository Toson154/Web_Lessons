using System.ComponentModel.DataAnnotations;

namespace Web_Lessons.ViewModels
{

    // For account profile
    public class UserProfileViewModel
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string? Bio { get; set; }

        public IFormFile? ProfileImage { get; set; }

        public string? CurrentProfileImageUrl { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }

    // For admin editing users
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public bool IsTeacher { get; set; }
        public bool IsActive { get; set; }
        public string[] SelectedRoles { get; set; }
    }
}