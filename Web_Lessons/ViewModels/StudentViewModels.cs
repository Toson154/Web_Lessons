using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Web_Lessons.Models;

namespace Web_Lessons.ViewModels
{
    public class StartChatRequest
    {
        public string TeacherId { get; set; }
        public int? CourseId { get; set; }
        public string InitialMessage { get; set; }
    }
    // في ViewModels/StudentViewModels.cs - أضف هذا النموذج الجديد
    public class DashboardNotificationViewModel
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
        public int? RelatedId { get; set; }
        public string RelatedType { get; set; }
    }
    // ViewModel للنشاطات الأخيرة
    public class ActivityViewModel
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Link { get; set; }
        public string TimeAgo { get; set; }
    }

    // ViewModel للدروس القادمة
    public class UpcomingLessonViewModel
    {
        public int LessonId { get; set; }
        public string Title { get; set; }
        public string CourseTitle { get; set; }
        public int Duration { get; set; }
        public int Order { get; set; }
        public bool IsCompleted { get; set; }
        public int CourseId { get; set; }
    }

    // ViewModel لإحصائيات الأسبوع
    public class WeeklyStatsViewModel
    {
        public List<string> Days { get; set; } = new List<string>();
        public List<int> CompletedLessons { get; set; } = new List<int>();
        public List<double> TimeSpent { get; set; } = new List<double>();
        public int TotalCompleted { get; set; }
        public double AverageTimePerDay { get; set; }
    }

    // ViewModel للمواد الدراسية
    public class SubjectStatsViewModel
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }
        public int CourseCount { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public int ProgressPercentage { get; set; }
    }

    // ViewModel الموسع للداشبورد
    public class StudentDashboardViewModel
    {
        // الأساسية
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string ProfileImageUrl { get; set; }
        public int EnrolledCoursesCount { get; set; }
        public int AvailableCoursesCount { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public int ProgressPercentage { get; set; }
        public DateTime JoinDate { get; set; }

        // القوائم
        public List<Course> EnrolledCourses { get; set; } = new List<Course>();
        public List<Course> AvailableCourses { get; set; } = new List<Course>();
        public List<ActivityViewModel> RecentActivities { get; set; } = new List<ActivityViewModel>();
        public List<UpcomingLessonViewModel> UpcomingLessons { get; set; } = new List<UpcomingLessonViewModel>();

        // الإحصائيات
        public int UnreadNotificationsCount { get; set; }
        public int UnreadMessagesCount { get; set; }
        public WeeklyStatsViewModel WeeklyStats { get; set; }
        public List<SubjectStatsViewModel> TopSubjects { get; set; } = new List<SubjectStatsViewModel>();
        public int AvailableLessonsCount { get; set; }
        public int UpcomingAssignmentsCount { get; set; }
        public int PendingChatsCount { get; set; }
        public int UnreadCommentsCount { get; set; }

        // Properties for navigation
        public Dictionary<int, string> CourseLinks { get; set; } = new();
        public Dictionary<int, string> LessonLinks { get; set; } = new();
        public Dictionary<int, string> SubjectLinks { get; set; } = new();

        // Real-time stats
        public int DailyLearningGoal { get; set; } = 60; // minutes
        public int TodayLearningTime { get; set; }
        public int LearningStreak { get; set; } // consecutive days
        // إضافية
        public bool IsPremium { get; set; }
        public int CurrentStreak { get; set; } // أيام متتالية من التعلم
    }

    // ViewModel لتصفح الدورات
    public class BrowseCourseViewModel
    {
        public Course Course { get; set; }
        public int StudentCount { get; set; }
        public int LessonCount { get; set; }
        public bool IsEnrolled { get; set; }
        public string TeacherName { get; set; }
        public string SubjectName { get; set; }
        public double? Rating { get; set; } // إذا كان هناك نظام تقييم
    }
    // في ViewModels/StudentViewModels.cs - تحديث الـ ViewModel
    public class StudentCourseDetailsViewModel
    {
        // إضافة قيمة افتراضية لتفادي Null Reference
        public Course Course { get; set; } = new Course();
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
        public int ProgressPercentage { get; set; }
        public double TimeSpentHours { get; set; }
        public List<Lesson> Lessons { get; set; } = new List<Lesson>();
        public Dictionary<int, bool> LessonCompletionStatus { get; set; } = new Dictionary<int, bool>();
        public List<CourseCommentViewModel> RecentComments { get; set; } = new List<CourseCommentViewModel>();
        public int TotalStudents { get; set; }
        public bool CanAccessChat { get; set; }
    }

    public class CourseCommentViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string UserName { get; set; } = "Unknown User";
        public string UserProfileImage { get; set; } = "/images/default-avatar.png";
        public string LessonTitle { get; set; } = "Unknown Lesson";
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } = "Just now";
    }

    // ViewModel الموسع لدورة معينة

    // ViewModel للتعليقات في صفحة الدورة

    // ViewModel الموسع للدرس
    public class StudentLessonViewModel
    {
        public Lesson Lesson { get; set; }
        public Course Course { get; set; }
        public bool IsCompleted { get; set; }
        public LessonProgress Progress { get; set; }
        public Lesson PreviousLesson { get; set; }
        public Lesson NextLesson { get; set; }

        // أضف هذه الخصائص:
        public List<CommentViewModel> Comments { get; set; } = new List<CommentViewModel>();
        public Dictionary<int, bool> LessonCompletionStatus { get; set; } = new Dictionary<int, bool>();
        public int TotalComments { get; set; }
        public int TotalStudentsCompleted { get; set; }
        public int CurrentLessonIndex { get; set; }
        public int TotalLessons { get; set; }

        // إحصائيات إضافية
        public int CourseProgress { get; set; }
        public int CompletedLessons { get; set; }
        public int TotalLessonsInCourse { get; set; }
        public string Notes { get; set; }

        // بيانات للمحادثات
        public bool HasUnreadMessages { get; set; }
        public int UnreadMessagesCount { get; set; }

        // بيانات للإشعارات
        public List<NotificationViewModel> RecentNotifications { get; set; } = new List<NotificationViewModel>();

        // بيانات للتفاعلات
        public Dictionary<int, string> UserReactions { get; set; } = new Dictionary<int, string>();
    }
    // ViewModel للتعليق مع التفاعلات
    public class LessonCommentViewModel
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
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public List<LessonCommentViewModel> Replies { get; set; } = new List<LessonCommentViewModel>();
        public int RepliesCount { get; set; }

        // التفاعلات
        public Dictionary<string, int> Reactions { get; set; } = new Dictionary<string, int>();
        public string CurrentUserReaction { get; set; }
        public List<UserReactionViewModel> ReactionDetails { get; set; } = new List<UserReactionViewModel>();

        // بيانات المذكور
        public string MentionedUserId { get; set; }
        public string MentionedUserName { get; set; }
    }
    // ViewModel لجزء التعليق
    public class CommentPartialViewModel
    {
        public CommentViewModel Comment { get; set; }
        public Dictionary<int, string> UserReactions { get; set; } = new Dictionary<int, string>();
        public string CurrentUserId { get; set; }
    }
    public class UserReactionViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserProfileImage { get; set; }
        public string ReactionType { get; set; }
        public DateTime ReactedAt { get; set; }
    }

    // ViewModel الموسع للملف الشخصي
    public class StudentProfileViewModel
    {
        public ApplicationUser User { get; set; }
        public DateTime JoinDate { get; set; }
        public DateTime? LastLogin { get; set; }
        public int EnrolledCoursesCount { get; set; }
        public int CompletedLessonsCount { get; set; }
        public int TotalCoursesCount { get; set; }
        public int OverallProgress { get; set; }
        public List<Course> RecentCourses { get; set; } = new List<Course>();
        public List<AchievementViewModel> RecentAchievements { get; set; } = new List<AchievementViewModel>();
        public string TotalLearningTime { get; set; }
    }

    // ViewModel للإنجازات
    public class AchievementViewModel
    {
        public string LessonTitle { get; set; }
        public string CourseTitle { get; set; }
        public DateTime CompletedAt { get; set; }
        public int TimeSpent { get; set; }
        public string TimeAgo { get; set; }
    }

    // ViewModel للإشعارات
    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
        public int? RelatedId { get; set; }
        public string RelatedType { get; set; }
    }

    // ViewModel لدورة الطالب
    public class StudentCourseViewModel
    {
        public Course Course { get; set; }
        public Enrollment Enrollment { get; set; }
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
        public int ProgressPercentage { get; set; }
        public double TimeSpentHours { get; set; }
        public string EnrollmentDateFormatted { get; set; }
        public DateTime? LastActivity { get; set; }

        // ✅ التصحيح - استخدم خاصية يمكن الوصول لها مباشرة
        public ApplicationUser Teacher { get; set; }

        // إضافة خصائص مساعدة لتجنب أخطاء Null
        public string TeacherName => Teacher?.FullName ?? "Unknown Teacher";
        public string TeacherEmail => Teacher?.Email ?? "";
        public string TeacherProfileImage => Teacher?.ProfileImageUrl ?? "/images/avatar.png";
        public string TeacherBioShort => Teacher?.Bio?.Length > 100
            ? Teacher.Bio.Substring(0, 100) + "..."
            : Teacher?.Bio ?? "";
    }    // ViewModel لتقدم الدورة
    public class CourseProgressViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public string SubjectName { get; set; }
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
        public int ProgressPercentage { get; set; }
        public string EnrollmentDate { get; set; }
    }
}