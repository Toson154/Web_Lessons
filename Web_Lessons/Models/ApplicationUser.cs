// In Models/ApplicationUser.cs or wherever your ApplicationUser class is
namespace Web_Lessons.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; }

    public bool IsTeacher { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public DateTime? LastLogin { get; set; }

    // Profile Image
    public string? ProfileImageUrl { get; set; }

    public string? Bio { get; set; }
    public bool? EmailNotificationsEnabled { get; set; } = true;
    public bool? PushNotificationsEnabled { get; set; } = true;
    public DateTime? LastNotificationCheck { get; set; }
    public string? TimeZone { get; set; } = "UTC";
    public bool? DarkModeEnabled { get; set; } = false;

    // Learning preferences
    public int? DailyLearningGoal { get; set; } = 60; // minutes
    public string? PreferredLearningTime { get; set; }
    public bool? AutoPlayNextLesson { get; set; } = true;
    public bool? ShowSubtitles { get; set; } = true;
    public decimal? PlaybackSpeed { get; set; } = 1.0m;

    // Navigation Properties
    public virtual ICollection<Subject>? Subjects { get; set; }
    public virtual ICollection<Enrollment>? Enrollments { get; set; }
    public virtual ICollection<LessonProgress>? LessonProgresses { get; set; }
    public virtual ICollection<Comment>? Comments { get; set; }
    public virtual ICollection<Notification>? Notifications { get; set; } // ADD THIS LINE
}
