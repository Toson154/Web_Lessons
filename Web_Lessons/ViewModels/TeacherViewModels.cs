// ViewModels/TeacherViewModels.cs
using System.ComponentModel.DataAnnotations;
using System.IO; // Add for Path class
using Web_Lessons.Models;

namespace Web_Lessons.ViewModels
{
    // في TeacherViewModels.cs
    public class TeacherDashboardViewModel
    {
        public int SubjectsCount { get; set; }
        public int CoursesCount { get; set; }
        public int LessonsCount { get; set; }
        public int StudentsCount { get; set; }
        public string TeacherName { get; set; }
        public int PublishedCourses { get; set; }
        public int DraftCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public DateTime? LastActivity { get; set; }
        public string LastActivityAgo { get; set; }

        // خصائص جديدة لدعم AJAX
        public List<object> RecentActivities { get; set; } = new List<object>();
        public List<object> TopCourses { get; set; } = new List<object>();
        public List<object> RecentStudents { get; set; } = new List<object>();
    }
    public class CreateSubjectViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Subject name is required")]
        [StringLength(100, ErrorMessage = "Subject name cannot exceed 100 characters")]
        [Display(Name = "Subject Name")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Subject Image")]
        public IFormFile? ImageFile { get; set; }
    }

    public class EditSubjectViewModel : CreateSubjectViewModel
    {
        public string? ExistingImageUrl { get; set; }
    }

    // Add missing EditCourseViewModel
    public class EditCourseViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Course title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [Display(Name = "Course Title")]
        public string Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Level is required")]
        [Display(Name = "Level")]
        public string Level { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        [Display(Name = "Subject")]
        public int SubjectId { get; set; }

        [Display(Name = "Publish Course")]
        public bool IsPublished { get; set; }

        [Display(Name = "Thumbnail URL")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? ThumbnailUrl { get; set; }

        [Display(Name = "Thumbnail Image")]
        public IFormFile? ThumbnailFile { get; set; }

        [Display(Name = "Current Thumbnail")]
        public string? ExistingThumbnailUrl { get; set; }
    }
    // Add to TeacherViewModels.cs
    public class DashboardStatsViewModel
    {
        public int TotalSubjects { get; set; }
        public int TotalCourses { get; set; }
        public int TotalLessons { get; set; }
        public int TotalStudents { get; set; }
        public int PublishedCourses { get; set; }
        public int DraftCourses { get; set; }
        public double AverageCourseCompletion { get; set; }
        public List<RecentEnrollmentViewModel> RecentEnrollments { get; set; }
        public List<RecentActivityViewModel> RecentActivities { get; set; }
    }

    public class RecentEnrollmentViewModel
    {
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string ProfileImageUrl { get; set; }
        public string CourseTitle { get; set; }
        public DateTime EnrolledDate { get; set; }
        public int ProgressPercentage { get; set; }
    }

    public class RecentActivityViewModel
    {
        public string Type { get; set; } // "Course", "Lesson", "Subject"
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime ActivityDate { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
    }

    public class TopCourseViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string SubjectName { get; set; }
        public string ThumbnailUrl { get; set; }
        public int StudentCount { get; set; }
        public int LessonCount { get; set; }
        public int ProgressPercentage { get; set; }
    }
    // Add missing CreateCourseViewModel
    public class CreateCourseViewModel
    {
        [Required(ErrorMessage = "Course title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [Display(Name = "Course Title")]
        public string Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Level is required")]
        [Display(Name = "Level")]
        public string Level { get; set; } = "Beginner";

        [Required(ErrorMessage = "Subject is required")]
        [Display(Name = "Subject")]
        public int SubjectId { get; set; }

        [Display(Name = "Publish Course")]
        public bool IsPublished { get; set; } = true;

        [Display(Name = "Thumbnail URL")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? ThumbnailUrl { get; set; }

        [Display(Name = "Thumbnail Image")]
        public IFormFile? ThumbnailFile { get; set; }
    }
    // In ViewModels/TeacherViewModels.cs
    public class EnrollmentViewModel
    {
        public int EnrollmentId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string? StudentProfileImage { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public string CourseLevel { get; set; }
        public string SubjectName { get; set; }
        public DateTime EnrolledAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public int ProgressPercentage { get; set; }
        public DateTime? LastActivity { get; set; }
    }
    // Also update CreateLessonViewModel to include PdfUrl
    public class TeacherStudentViewModel
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string? ProfileImageUrl { get; set; }
        public List<CourseEnrollmentViewModel> Enrollments { get; set; }
        public int TotalEnrolledCourses { get; set; }
        public int TotalCompletedLessons { get; set; }
        public DateTime? LastActivity { get; set; }
    }
    public class CourseEnrollmentViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public string SubjectName { get; set; }
        public DateTime EnrolledAt { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public int ProgressPercentage { get; set; }
    }

    // في CreateLessonViewModel.cs
    // في CreateLessonViewModel.cs
    public class CreateLessonViewModel
    {
        // إضافة Id للتعديل
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a course")]
        [Display(Name = "Course")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Lesson title is required")]
        [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters")]
        [Display(Name = "Lesson Title")]
        public string Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Duration is required")]
        [Range(1, 300, ErrorMessage = "Duration must be between 1 and 300 minutes")]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; }

        [Required(ErrorMessage = "Order is required")]
        [Range(1, 1000, ErrorMessage = "Order must be between 1 and 1000")]
        [Display(Name = "Order in Course")]
        public int Order { get; set; }

        [Display(Name = "Video URL")]
        public string VideoUrl { get; set; }

        [Display(Name = "PDF Notes")]
        public IFormFile PdfFile { get; set; }

        [Display(Name = "PDF URL")]
        public string PdfUrl { get; set; }

        public bool IsEditMode { get; set; } = false;
    }
    public class EditLessonViewModel : CreateLessonViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Current Video URL")]
        public string? ExistingVideoUrl { get; set; }

        [Display(Name = "Current PDF URL")]
        public string? ExistingPdfUrl { get; set; }

        // Add this property to handle existing PDF
        [Display(Name = "PDF URL (Optional)")]
        public string? PdfUrl { get; set; }
    }
    // ViewModel for Students
    public class StudentViewModel
    {
        public ApplicationUser Student { get; set; }
        public List<Enrollment> Enrollments { get; set; }
        public int TotalCourses { get; set; }
        public double ProgressPercentage { get; set; }
        public DateTime? LastActivity { get; set; }
    }
    // في ViewModels/TeacherViewModels.cs - أضف
    public class TeacherLessonViewModel
    {
        public Lesson Lesson { get; set; }
        public Course Course { get; set; }
        public int TotalStudents { get; set; }
        public List<ApplicationUser> EnrolledStudents { get; set; } = new List<ApplicationUser>();
        public int CommentsCount { get; set; }
        public bool HasPdf { get; set; }
        public List<Lesson> CourseLessons { get; set; } = new List<Lesson>();

        // إضافة هذه الخصائص الجديدة
        public List<TeacherCommentViewModel> Comments { get; set; } = new List<TeacherCommentViewModel>();
        public TeacherNotesViewModel TeacherNotes { get; set; }
        public string CurrentUserId { get; set; }
        public int UnreadMessagesCount { get; set; }
        public bool HasUnreadMessages { get; set; }
    }    // في ViewModels/TeacherViewModels.cs - إضافة
    public class TeacherNotesViewModel
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public string TeacherId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string LastUpdated { get; set; }
    }
    // في ViewModels/TeacherViewModels.cs - إضافة
    public class AddTeacherReplyViewModel
    {
        [Required]
        public int CommentId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Content { get; set; }
    }
    public class AddTeacherCommentViewModel
    {
        [Required]
        public int LessonId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }

        public int? ParentCommentId { get; set; }
        public string MentionedUserId { get; set; }
    }
    public class TeacherCommentViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserProfileImage { get; set; }
        public bool IsTeacher { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public List<TeacherReplyViewModel> Replies { get; set; } = new List<TeacherReplyViewModel>();
        public Dictionary<string, int> Reactions { get; set; } = new Dictionary<string, int>();
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanReply { get; set; } = true;
    }

    public class TeacherReplyViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserProfileImage { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
        public bool IsEdited { get; set; }
    }

    public class TeacherChatViewModel
    {
        public int Id { get; set; }
        public string OtherUserId { get; set; }
        public string OtherUserName { get; set; }
        public string OtherUserProfileImage { get; set; }
        public bool IsTeacher { get; set; }
        public string LastMessage { get; set; }
        public int? UnreadCount { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public bool IsOnline { get; set; }
    }

    public class TeacherChatMessageViewModel
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public string Content { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string SenderProfileImage { get; set; }
        public bool IsOwnMessage { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
    }

    public class TeacherChatDetailsViewModel
    {
        public TeacherChatViewModel Chat { get; set; }
        public List<TeacherChatMessageViewModel> Messages { get; set; } = new List<TeacherChatMessageViewModel>();
        public SendMessageViewModel NewMessage { get; set; } = new SendMessageViewModel();
    }
    // ViewModel for displaying students in a course
    public class TeacherStudentsViewModel
    {
        public int? CourseId { get; set; }
        public string CourseTitle { get; set; }
        public List<StudentViewModel> Students { get; set; }
        public List<Course> TeacherCourses { get; set; }
    }
}