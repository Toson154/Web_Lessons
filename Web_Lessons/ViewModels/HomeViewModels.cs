// ViewModels/HomeViewModels.cs
using System;
using System.Collections.Generic;

namespace Web_Lessons.ViewModels
{
    public class FeaturedTeacherViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ProfileImage { get; set; }
        public string Bio { get; set; }
        public string Subject { get; set; }
        public int StudentCount { get; set; }
        public int CourseCount { get; set; }
        public double Rating { get; set; }
    }
    public class HomePageViewModel
    {
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalLessons { get; set; }
        public int RecentEnrollmentsCount { get; set; }
        public List<FeaturedCourseViewModel> FeaturedCourses { get; set; } = new();
        public List<SubjectViewModel> PopularSubjects { get; set; } = new();
        public List<TestimonialViewModel> Testimonials { get; set; } = new();
        public List<FeatureViewModel> Features { get; set; } = new();
        public List<FeaturedTeacherViewModel> FeaturedTeachers { get; set; } = new();
    }
    public class FeaturedCourseViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Level { get; set; }
        public string ThumbnailUrl { get; set; }
        public string SubjectName { get; set; }
        public int StudentCount { get; set; }
        public int LessonCount { get; set; }
        public string TeacherName { get; set; }
        public string TeacherProfileImage { get; set; }
        public double Rating { get; set; } = 4.5;
    }

    public class SubjectViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int CourseCount { get; set; }
        public int EnrollmentCount { get; set; } // أضف هذا الحقل الجديد
    }
    public class TestimonialViewModel
    {
        public string StudentName { get; set; }
        public string StudentImage { get; set; }
        public string Content { get; set; }
        public string CourseName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo => GetTimeAgo(CreatedAt);

        private string GetTimeAgo(DateTime date)
        {
            var span = DateTime.UtcNow - date;
            if (span.TotalDays >= 365) return $"{(int)(span.TotalDays / 365)} years ago";
            if (span.TotalDays >= 30) return $"{(int)(span.TotalDays / 30)} months ago";
            if (span.TotalDays >= 7) return $"{(int)(span.TotalDays / 7)} weeks ago";
            if (span.TotalDays >= 1) return $"{(int)span.TotalDays} days ago";
            if (span.TotalHours >= 1) return $"{(int)span.TotalHours} hours ago";
            return "recently";
        }
    }

    public class FeatureViewModel
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class ContactFormViewModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    }
}