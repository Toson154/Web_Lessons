// ViewModels/AdminViewModels.cs
using System.ComponentModel.DataAnnotations;
using Web_Lessons.Models;

namespace Web_Lessons.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalCourses { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalLessons { get; set; }
        public List<ApplicationUser>? RecentUsers { get; set; }
    }

    public class AdminStatisticsViewModel
    {
        public int TotalUsers { get; set; }
        public int UsersThisMonth { get; set; }
        public int TeachersCount { get; set; }
        public int StudentsCount { get; set; }
        public int TotalCourses { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalLessons { get; set; }
        public int TotalEnrollments { get; set; }
        public int ActiveUsers { get; set; }
        public List<Enrollment>? RecentEnrollments { get; set; }
        public List<Course>? RecentCourses { get; set; }
    }

    public class SystemSettingsViewModel
    {
        [Required]
        public string SiteName { get; set; } = "Web Lessons";

        [Required]
        [EmailAddress]
        public string SiteEmail { get; set; } = "admin@weblessons.com";

        [EmailAddress]
        public string? ContactEmail { get; set; }

        public string? ContactPhone { get; set; }
        public string? Address { get; set; }

        public bool AllowRegistrations { get; set; } = true;
        public bool RequireEmailConfirmation { get; set; } = false;

        [Range(1, 1000)]
        public int MaxFileSizeMB { get; set; } = 100;

        public string AllowedFileTypes { get; set; } = ".mp4,.mov,.avi,.pdf,.docx,.jpg,.jpeg,.png,.gif";
    }
    // In ViewModels/AdminViewModels.cs
    public class AdminCourseViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Level { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SubjectName { get; set; }
        public string TeacherName { get; set; }
        public string TeacherEmail { get; set; }
        public int LessonsCount { get; set; }
        public int EnrollmentsCount { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}
