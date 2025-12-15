// Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_Lessons.Models;
using Web_Lessons.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace Web_Lessons.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // الصفحة الرئيسية للزوار غير المسجلين
// تحديث إجراء Index ليشمل المدرسين
public async Task<IActionResult> Index()
{
    // إذا كان المستخدم مسجل دخول، إعادة توجيه للداشبورد المناسب
    if (User.Identity.IsAuthenticated)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user != null)
        {
            if (user.IsTeacher)
                return RedirectToAction("Dashboard", "Teacher");
            else
                return RedirectToAction("Dashboard", "Student");
        }
    }

    var model = new HomePageViewModel();

    try
    {

        // جلب الإحصائيات الأساسية
        model.TotalCourses = await _context.Courses
            .CountAsync(c => c.IsPublished);

        model.TotalStudents = await _context.Users
            .CountAsync(u => !u.IsTeacher);

        model.TotalTeachers = await _context.Users
            .CountAsync(u => u.IsTeacher);

        model.TotalLessons = await _context.Lessons
            .CountAsync();

        model.RecentEnrollmentsCount = await _context.Enrollments
            .CountAsync(e => e.EnrolledAt >= DateTime.UtcNow.AddDays(-7));

        // جلب الدورات المميزة
        model.FeaturedCourses = await GetFeaturedCourses();

        // جلب المواد الشائعة
        model.PopularSubjects = await GetPopularSubjects();

        // جلب المدرسين المميزين
        model.FeaturedTeachers = await GetFeaturedTeachers();

        // Features Section
        model.Features = new List<FeatureViewModel>
        {
            new FeatureViewModel
            {
                Icon = "fas fa-video",
                Title = "Video Lessons",
                Description = "High-quality video lessons from expert instructors"
            },
            new FeatureViewModel
            {
                Icon = "fas fa-certificate",
                Title = "Certificates",
                Description = "Get certified upon course completion"
            },
            new FeatureViewModel
            {
                Icon = "fas fa-chart-line",
                Title = "Progress Tracking",
                Description = "Monitor your learning journey with detailed analytics"
            },
            new FeatureViewModel
            {
                Icon = "fas fa-headset",
                Title = "24/7 Support",
                Description = "Get help whenever you need it"
            }
        };
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading home data: {ex.Message}");
    }

    return View(model);
}
        private async Task<List<FeaturedCourseViewModel>> GetFeaturedCourses()
        {
            return await _context.Courses
                .Include(c => c.Subject)
                    .ThenInclude(s => s.Teacher)
                .Include(c => c.Lessons)
                .Include(c => c.Enrollments)
                .Where(c => c.IsPublished)
                .OrderByDescending(c => c.Enrollments.Count)
                .Take(4)
                .Select(c => new FeaturedCourseViewModel
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Level = c.Level,
                    ThumbnailUrl = c.ThumbnailUrl ?? "/images/default-course.jpg",
                    SubjectName = c.Subject.Name,
                    StudentCount = c.Enrollments.Count,
                    LessonCount = c.Lessons.Count,
                    TeacherName = c.Subject.Teacher.FullName,
                    TeacherProfileImage = c.Subject.Teacher.ProfileImageUrl ?? "/images/avatar.png"
                })
                .ToListAsync();
        }

        private async Task<List<SubjectViewModel>> GetPopularSubjects()
        {
            // طريقة أبسط بدون Sum داخل Select
            var subjects = await _context.Subjects
                .Include(s => s.Courses)
                    .ThenInclude(c => c.Enrollments)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    s.ImageUrl,
                    Courses = s.Courses,
                    CourseCount = s.Courses.Count
                })
                .ToListAsync();

            return subjects.Select(s => new SubjectViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                ImageUrl = s.ImageUrl ?? "/images/default-subject.jpg",
                CourseCount = s.CourseCount,
                EnrollmentCount = s.Courses.Sum(c => c.Enrollments?.Count ?? 0)
            })
            .OrderByDescending(s => s.EnrollmentCount)
            .Take(6)
            .ToList();
        }
        private async Task<List<FeaturedTeacherViewModel>> GetFeaturedTeachers()
        {
            return await _context.Users
                .Where(u => u.IsTeacher)
                .Select(t => new FeaturedTeacherViewModel
                {
                    Id = t.Id,
                    Name = t.FullName,
                    ProfileImage = t.ProfileImageUrl ?? "/images/avatar.png",
                    Bio = t.Bio ?? "Expert instructor with years of experience",
                    Subject = "Multiple Subjects",
                    StudentCount = _context.Enrollments
                        .Count(e => e.Course.Subject.TeacherId == t.Id),
                    CourseCount = _context.Courses
                        .Count(c => c.Subject.TeacherId == t.Id),
                    Rating = 4.8 // افتراضي
                })
                .OrderByDescending(t => t.StudentCount)
                .Take(4)
                .ToListAsync();
        }

        private async Task<List<TestimonialViewModel>> GetTestimonials()
        {
            // أخذ تعليقات عشوائية كشهادات
            return await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Lesson)
                    .ThenInclude(l => l.Course)
                .Where(c => !c.IsDeleted && c.User.IsTeacher == false)
                .OrderByDescending(c => c.CreatedAt)
                .Take(4)
                .Select(c => new TestimonialViewModel
                {
                    StudentName = c.User.FullName,
                    StudentImage = c.User.ProfileImageUrl ?? "/images/avatar.png",
                    Content = c.Content.Length > 120 ?
                             c.Content.Substring(0, 120) + "..." : c.Content,
                    CourseName = c.Lesson.Course.Title,
                    //Rating = 5, // افتراضي
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
        }
        public async Task<IActionResult> About()
        {
            var stats = new
            {
                TotalCourses = await _context.Courses.CountAsync(c => c.IsPublished),
                ActiveStudents = await _context.Enrollments
                    .Select(e => e.StudentId)
                    .Distinct()
                    .CountAsync(),
                TotalHours = await _context.Lessons.SumAsync(l => l.DurationMinutes) / 60,
                SuccessRate = 95 // نسبة افتراضية
            };

            return View(stats);
        }

        // صفحة الاتصال
        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Contact(ContactFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                // هنا يمكن حفظ الرسالة في قاعدة البيانات
                TempData["SuccessMessage"] = "Thank you for your message! We'll contact you soon.";
                return RedirectToAction("Contact");
            }
            return View(model);
        }
    }
}