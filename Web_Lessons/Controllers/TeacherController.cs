using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Web_Lessons.Models;
using Web_Lessons.ViewModels;
using System.IO;
using Web_Lessons.Hubs;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Web_Lessons.Hubs;

namespace Web_Lessons.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly ILogger<TeacherController> _logger; // أضف هذا

        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHubContext<ChatHub> _hubContext; // أضف هذا


        public TeacherController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<TeacherController> logger,
            IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger; // Initialize logger
            _hubContext = hubContext; // Initialize it
            _webHostEnvironment = webHostEnvironment;
        }
        // في TeacherController.cs - أضف هذه الدالة
        [HttpPost]
        [RequestSizeLimit(2147483648)] // 2GB
        public async Task<IActionResult> UploadVideo(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("Upload attempt with null or empty file");
                    return Json(new { success = false, message = "No file selected" });
                }

                // التحقق من نوع الملف
                var allowedExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm", ".wmv" };
                var extension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning("Invalid video format: {FileName}", file.FileName);
                    return Json(new { success = false, message = $"Invalid video format. Allowed: {string.Join(", ", allowedExtensions)}" });
                }

                // التحقق من الحجم (2GB)
                if (file.Length > 2147483648)
                {
                    _logger.LogWarning("File too large: {FileName}, Size: {Size}", file.FileName, file.Length);
                    return Json(new { success = false, message = "File is too large. Maximum size is 2GB" });
                }

                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "videos");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                    _logger.LogInformation("Created videos directory: {Directory}", uploadsFolder);
                }

                // إنشاء اسم فريد للملف
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // حفظ الملف
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var videoUrl = $"/uploads/videos/{uniqueFileName}";

                _logger.LogInformation("Video uploaded successfully: {FilePath}, URL: {VideoUrl}, Size: {Size}",
                    filePath, videoUrl, file.Length);

                return Json(new
                {
                    success = true,
                    videoUrl = videoUrl,
                    fileName = uniqueFileName,
                    originalName = file.FileName,
                    size = file.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading video");
                return Json(new { success = false, message = $"Upload failed: {ex.Message}" });
            }
        }

        // في TeacherController.cs - تحديث دالة Dashboard
        // في TeacherController.cs - تصحيح إجراء Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // التحقق من أن المستخدم مصادق عليه أولاً
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Account");
                }

                // الحصول على ID المعلم - استخدام الطريقة الآمنة
                var teacherId = _userManager.GetUserId(User);

                if (string.IsNullOrEmpty(teacherId))
                {
                    _logger.LogWarning("Teacher ID is null or empty. User: {User}", User.Identity.Name);
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation("Loading dashboard for teacher: {TeacherId}", teacherId);

                // الحصول على بيانات المعلم
                var teacher = await _userManager.GetUserAsync(User);
                if (teacher == null)
                {
                    _logger.LogWarning("Teacher not found for ID: {TeacherId}", teacherId);
                    return RedirectToAction("Login", "Account");
                }

                // الحصول على الإحصائيات - مع معالجة الاستثناءات
                int subjectsCount = 0, coursesCount = 0, lessonsCount = 0, studentsCount = 0;

                try
                {
                    subjectsCount = await _context.Subjects
                        .CountAsync(s => s.TeacherId == teacherId);

                    coursesCount = await _context.Courses
                        .CountAsync(c => c.Subject.TeacherId == teacherId);

                    lessonsCount = await _context.Lessons
                        .CountAsync(l => l.Course.Subject.TeacherId == teacherId);

                    studentsCount = await _context.Enrollments
                        .Where(e => e.Course.Subject.TeacherId == teacherId)
                        .Select(e => e.StudentId)
                        .Distinct()
                        .CountAsync();

                    _logger.LogInformation("Dashboard stats loaded - Subjects: {Subjects}, Courses: {Courses}, Lessons: {Lessons}, Students: {Students}",
                        subjectsCount, coursesCount, lessonsCount, studentsCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading dashboard statistics for teacher {TeacherId}", teacherId);
                    // نواصل مع القيم الافتراضية (0) بدلاً من إعادة الخطأ
                }

                // إنشاء النموذج
                var model = new TeacherDashboardViewModel
                {
                    SubjectsCount = subjectsCount,
                    CoursesCount = coursesCount,
                    LessonsCount = lessonsCount,
                    StudentsCount = studentsCount,
                    TeacherName = teacher?.FullName?.Split(' ').FirstOrDefault() ?? teacher?.FullName ?? "Teacher"
                };

                // حفظ البيانات مؤقتاً للعرض
                ViewData["DashboardData"] = model;
                ViewData["TeacherId"] = teacherId;
                ViewData["TeacherName"] = teacher?.FullName;
                ViewData["TeacherEmail"] = teacher?.Email;
                ViewData["TeacherProfileImage"] = teacher?.ProfileImageUrl ?? "/images/teacher-default.png";

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in Teacher Dashboard action");

                // في حالة الخطأ، نعيد نموذجاً افتراضياً
                return View(new TeacherDashboardViewModel
                {
                    SubjectsCount = 0,
                    CoursesCount = 0,
                    LessonsCount = 0,
                    StudentsCount = 0,
                    TeacherName = "Teacher"
                });
            }
        }
        // دالة AJAX جديدة للحصول على الإحصائيات
        [HttpGet]
        public async Task<JsonResult> GetDashboardStats()
        {
            try
            {
                // التحقق من المصادقة
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // الحصول على ID المعلم
                var teacherId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(teacherId))
                {
                    return Json(new { success = false, message = "Teacher ID not found" });
                }

                _logger.LogInformation("Getting dashboard stats for teacher: {TeacherId}", teacherId);

                // الحصول على الإحصائيات الأساسية
                var subjectsCount = await _context.Subjects
                    .CountAsync(s => s.TeacherId == teacherId);

                var coursesCount = await _context.Courses
                    .CountAsync(c => c.Subject.TeacherId == teacherId);

                var lessonsCount = await _context.Lessons
                    .CountAsync(l => l.Course.Subject.TeacherId == teacherId);

                var studentsCount = await _context.Enrollments
                    .Where(e => e.Course.Subject.TeacherId == teacherId)
                    .Select(e => e.StudentId)
                    .Distinct()
                    .CountAsync();

                // الحصول على الدورات المنشورة
                var publishedCourses = await _context.Courses
                    .CountAsync(c => c.Subject.TeacherId == teacherId && c.IsPublished);

                // الحصول على البيانات الإضافية
                var recentActivity = await GetRecentActivityData(teacherId);
                var topCourses = await GetTopCoursesData(teacherId);
                var recentStudents = await GetRecentStudentsData(teacherId);

                var stats = new
                {
                    SubjectsCount = subjectsCount,
                    CoursesCount = coursesCount,
                    LessonsCount = lessonsCount,
                    StudentsCount = studentsCount,
                    PublishedCourses = publishedCourses,
                    RecentActivity = recentActivity,
                    TopCourses = topCourses,
                    RecentStudents = recentStudents,
                    LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")
                };

                _logger.LogInformation("Dashboard stats retrieved successfully for teacher: {TeacherId}", teacherId);

                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return Json(new
                {
                    success = false,
                    message = "Error loading dashboard data",
                    data = new
                    {
                        SubjectsCount = 0,
                        CoursesCount = 0,
                        LessonsCount = 0,
                        StudentsCount = 0,
                        PublishedCourses = 0,
                        RecentActivity = new List<object>(),
                        TopCourses = new List<object>(),
                        RecentStudents = new List<object>(),
                        LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")
                    }
                });
            }
        }

        private async Task<object> GetRecentActivityData(string teacherId)
        {
            try
            {
                var activities = new List<object>();
                var lastWeek = DateTime.UtcNow.AddDays(-7);

                // 1. الدورات الجديدة (آخر 7 أيام)
                var recentCourses = await _context.Courses
                    .Include(c => c.Subject)
                    .Where(c => c.Subject.TeacherId == teacherId &&
                               c.CreatedAt >= lastWeek)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .Select(c => new
                    {
                        Type = "Course",
                        Title = c.Title,
                        Date = c.CreatedAt,
                        Description = $"Created new course: {c.Title}",
                        Icon = "fas fa-graduation-cap",
                        Color = "primary"
                    })
                    .ToListAsync();

                activities.AddRange(recentCourses);

                // 2. الدروس الجديدة (آخر 7 أيام)
                var recentLessons = await _context.Lessons
                    .Include(l => l.Course)
                    .ThenInclude(c => c.Subject)
                    .Where(l => l.Course.Subject.TeacherId == teacherId &&
                               l.CreatedAt >= lastWeek)
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(5)
                    .Select(l => new
                    {
                        Type = "Lesson",
                        Title = l.Title,
                        Date = l.CreatedAt,
                        Description = $"Added new lesson: {l.Title}",
                        Icon = "fas fa-video",
                        Color = "success"
                    })
                    .ToListAsync();

                activities.AddRange(recentLessons);

                // 3. تسجيلات الطلاب الجدد
                var recentEnrollments = await _context.Enrollments
                    .Include(e => e.Course)
                    .ThenInclude(c => c.Subject)
                    .Include(e => e.Student)
                    .Where(e => e.Course.Subject.TeacherId == teacherId &&
                               e.EnrolledAt >= lastWeek)
                    .OrderByDescending(e => e.EnrolledAt)
                    .Take(5)
                    .Select(e => new
                    {
                        Type = "Enrollment",
                        Title = e.Student.FullName,
                        Date = e.EnrolledAt,
                        Description = $"New student enrolled in {e.Course.Title}",
                        Icon = "fas fa-user-plus",
                        Color = "warning"
                    })
                    .ToListAsync();

                activities.AddRange(recentEnrollments);

                // ترتيب حسب التاريخ وتحديد أول 8 أنشطة فقط
                return activities
                    .OrderByDescending(a => ((dynamic)a).Date)
                    .Take(8)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activity data");
                return new List<object>();
            }
        }

        private async Task<object> GetTopCoursesData(string teacherId)
        {
            try
            {
                return await _context.Courses
                    .Include(c => c.Subject)
                    .Include(c => c.Enrollments)
                    .Include(c => c.Lessons)
                    .Where(c => c.Subject.TeacherId == teacherId)
                    .OrderByDescending(c => c.Enrollments.Count)
                    .Take(4)
                    .Select(c => new
                    {
                        Id = c.Id,
                        Title = c.Title,
                        ThumbnailUrl = c.ThumbnailUrl ?? "/images/default-course.jpg",
                        SubjectName = c.Subject.Name,
                        StudentCount = c.Enrollments.Count,
                        LessonCount = c.Lessons.Count,
                        Progress = CalculateCourseProgress(c.Id, teacherId)
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top courses data");
                return new List<object>();
            }
        }

        private async Task<object> GetRecentStudentsData(string teacherId)
        {
            try
            {
                var lastMonth = DateTime.UtcNow.AddDays(-30);

                return await _context.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Course)
                    .ThenInclude(c => c.Subject)
                    .Where(e => e.Course.Subject.TeacherId == teacherId &&
                               e.EnrolledAt >= lastMonth)
                    .OrderByDescending(e => e.EnrolledAt)
                    .Take(10)
                    .Select(e => new
                    {
                        StudentId = e.StudentId,
                        StudentName = e.Student.FullName,
                        StudentEmail = e.Student.Email,
                        ProfileImageUrl = e.Student.ProfileImageUrl ?? "/images/avatar.png",
                        CourseTitle = e.Course.Title,
                        CourseId = e.CourseId,
                        EnrolledAt = e.EnrolledAt,
                        EnrolledDate = e.EnrolledAt.ToString("MMM dd"),
                        Progress = _context.LessonProgresses
                            .Count(lp => lp.StudentId == e.StudentId &&
                                       lp.Lesson.CourseId == e.CourseId &&
                                       lp.IsCompleted)
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent students data");
                return new List<object>();
            }
        }

        private int CalculateCourseProgress(int courseId, string teacherId)
        {
            try
            {
                var totalLessons = _context.Lessons.Count(l => l.CourseId == courseId);
                if (totalLessons == 0) return 0;

                var totalEnrollments = _context.Enrollments
                    .Count(e => e.CourseId == courseId);

                if (totalEnrollments == 0) return 0;

                var totalCompletedLessons = _context.Enrollments
                    .Where(e => e.CourseId == courseId)
                    .SelectMany(e => _context.LessonProgresses
                        .Where(lp => lp.StudentId == e.StudentId &&
                                   lp.Lesson.CourseId == courseId &&
                                   lp.IsCompleted))
                    .Count();

                var totalPossibleLessons = totalLessons * totalEnrollments;
                return totalPossibleLessons > 0 ?
                    (int)Math.Round((double)totalCompletedLessons / totalPossibleLessons * 100) : 0;
            }
            catch
            {
                return 0;
            }
        }
        // ========== SUBJECTS ==========
        public async Task<IActionResult> Subjects()
        {
            var teacherId = _userManager.GetUserId(User);
            var subjects = await _context.Subjects
                .Where(s => s.TeacherId == teacherId)
                .Include(s => s.Courses)
                .ToListAsync();

            return View("Subjects/Index", subjects);
        }

        [HttpGet]
        public IActionResult CreateSubject()
        {
            return View("Subjects/Create");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubject(CreateSubjectViewModel model)
        {
            if (ModelState.IsValid)
            {
                var teacherId = _userManager.GetUserId(User);

                var subject = new Subject
                {
                    Name = model.Name,
                    Description = model.Description,
                    TeacherId = teacherId,
                    CreatedAt = DateTime.UtcNow
                };

                // Handle image upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    subject.ImageUrl = await SaveUploadedFile(model.ImageFile, "subject-images");
                }
                else
                {
                    subject.ImageUrl = "/images/default-subject.jpg";
                }

                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Subject created successfully!";
                return RedirectToAction(nameof(Subjects));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditSubject(int id)
        {
            var teacherId = _userManager.GetUserId(User);
            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == id && s.TeacherId == teacherId);

            if (subject == null)
                return NotFound();

            var model = new EditSubjectViewModel
            {
                Id = subject.Id,
                Name = subject.Name,
                Description = subject.Description,
                ExistingImageUrl = subject.ImageUrl
            };

            return View("Subjects/Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubject(int id, EditSubjectViewModel model)
        {
            if (ModelState.IsValid)
            {
                var teacherId = _userManager.GetUserId(User);
                var subject = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.Id == id && s.TeacherId == teacherId);

                if (subject == null)
                    return NotFound();

                subject.Name = model.Name;
                subject.Description = model.Description;
                subject.UpdatedAt = DateTime.UtcNow;

                // Handle image upload - only update if new file provided
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    subject.ImageUrl = await SaveUploadedFile(model.ImageFile, "subject-images");
                }
                // If no new file and no existing URL, set default
                else if (string.IsNullOrEmpty(subject.ImageUrl))
                {
                    subject.ImageUrl = "/images/default-subject.jpg";
                }

                _context.Subjects.Update(subject);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Subject updated successfully!";
                return RedirectToAction(nameof(Subjects));
            }

            return View("Subjects/Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var teacherId = _userManager.GetUserId(User);
            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == id && s.TeacherId == teacherId);

            if (subject == null)
                return NotFound();

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Subject deleted successfully!";
            return RedirectToAction(nameof(Subjects));
        }

        // ========== COURSES ==========
        public async Task<IActionResult> Courses(int? subjectId)
        {
            var teacherId = _userManager.GetUserId(User);

            var coursesQuery = _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.Lessons)
                .Where(c => c.Subject.TeacherId == teacherId);

            if (subjectId.HasValue)
            {
                coursesQuery = coursesQuery.Where(c => c.SubjectId == subjectId);
                ViewBag.SubjectName = await _context.Subjects
                    .Where(s => s.Id == subjectId)
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync();
            }

            var courses = await coursesQuery.ToListAsync();
            ViewBag.SubjectId = subjectId;
            ViewBag.Subjects = await _context.Subjects
                .Where(s => s.TeacherId == teacherId)
                .ToListAsync();

            return View("Courses/Index", courses);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCourse()
        {
            var teacherId = _userManager.GetUserId(User);

            // Get subjects for dropdown - make sure to create SelectListItem properly
            var subjects = await _context.Subjects
                .Where(s => s.TeacherId == teacherId)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync();

            // Check if teacher has any subjects
            if (!subjects.Any())
            {
                TempData["ErrorMessage"] = "You must create a subject first before creating courses.";
                return RedirectToAction(nameof(Subjects));
            }

            // Assign to ViewBag.Subjects
            ViewBag.Subjects = subjects;

            // Add levels to ViewBag
            ViewBag.Levels = new List<SelectListItem>
            {
                new SelectListItem { Value = "Beginner", Text = "Beginner" },
                new SelectListItem { Value = "Intermediate", Text = "Intermediate" },
                new SelectListItem { Value = "Advanced", Text = "Advanced" }
            };

            return View("Courses/Create");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(CreateCourseViewModel model)
        {
            if (ModelState.IsValid)
            {
                var teacherId = _userManager.GetUserId(User);

                // Verify the subject belongs to this teacher
                var subject = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.Id == model.SubjectId && s.TeacherId == teacherId);

                if (subject == null)
                {
                    ModelState.AddModelError("SubjectId", "Subject not found or you don't have permission.");
                    await LoadCourseDropdownsAsync(teacherId);
                    return View(model);
                }

                var course = new Course
                {
                    Title = model.Title,
                    Description = model.Description,
                    Level = model.Level,
                    SubjectId = model.SubjectId,
                    IsPublished = model.IsPublished,
                    CreatedAt = DateTime.UtcNow,
                    ThumbnailUrl = model.ThumbnailUrl
                };

                // Handle thumbnail upload
                if (model.ThumbnailFile != null && model.ThumbnailFile.Length > 0)
                {
                    course.ThumbnailUrl = await SaveUploadedFile(model.ThumbnailFile, "course-images");
                }
                else if (string.IsNullOrEmpty(course.ThumbnailUrl))
                {
                    course.ThumbnailUrl = "/images/default-course.jpg";
                }

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Course created successfully!";
                return RedirectToAction(nameof(Courses));
            }

            // If model state is invalid, reload dropdowns
            var teacherIdForDropdowns = _userManager.GetUserId(User);
            await LoadCourseDropdownsAsync(teacherIdForDropdowns);

            return View(model);
        }

        private async Task LoadCourseDropdownsAsync(string teacherId)
        {
            var subjects = await _context.Subjects
                .Where(s => s.TeacherId == teacherId)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync();

            ViewBag.Subjects = subjects;

            ViewBag.Levels = new List<SelectListItem>
            {
                new SelectListItem { Value = "Beginner", Text = "Beginner" },
                new SelectListItem { Value = "Intermediate", Text = "Intermediate" },
                new SelectListItem { Value = "Advanced", Text = "Advanced" }
            };
        }

        [HttpGet]
        public async Task<IActionResult> EditCourse(int id)
        {
            var teacherId = _userManager.GetUserId(User);

            var course = await _context.Courses
                .Include(c => c.Subject)
                .FirstOrDefaultAsync(c => c.Id == id && c.Subject.TeacherId == teacherId);

            if (course == null)
            {
                TempData["ErrorMessage"] = "Course not found or you don't have permission.";
                return RedirectToAction(nameof(Courses));
            }

            var model = new EditCourseViewModel
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                Level = course.Level,
                SubjectId = course.SubjectId,
                IsPublished = course.IsPublished,
                ThumbnailUrl = course.ThumbnailUrl,
                ExistingThumbnailUrl = course.ThumbnailUrl
            };

            // Get teacher's subjects for dropdown
            var teacherSubjects = await _context.Subjects
                .Where(s => s.TeacherId == teacherId)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name,
                    Selected = s.Id == course.SubjectId
                })
                .ToListAsync();

            ViewBag.Subjects = teacherSubjects;

            ViewBag.Levels = new List<SelectListItem>
            {
                new SelectListItem { Value = "Beginner", Text = "Beginner", Selected = course.Level == "Beginner" },
                new SelectListItem { Value = "Intermediate", Text = "Intermediate", Selected = course.Level == "Intermediate" },
                new SelectListItem { Value = "Advanced", Text = "Advanced", Selected = course.Level == "Advanced" }
            };

            return View("Courses/Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(int id, EditCourseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload dropdowns if model is invalid
                var teacherId = _userManager.GetUserId(User);
                await LoadCourseDropdownsAsync(teacherId);
                return View("Courses/Edit", model);
            }

            var teacherIdForUpdate = _userManager.GetUserId(User);
            var course = await _context.Courses
                .Include(c => c.Subject)
                .FirstOrDefaultAsync(c => c.Id == id && c.Subject.TeacherId == teacherIdForUpdate);

            if (course == null)
            {
                TempData["ErrorMessage"] = "Course not found or you don't have permission.";
                return RedirectToAction(nameof(Courses));
            }

            // Verify the subject belongs to this teacher
            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == model.SubjectId && s.TeacherId == teacherIdForUpdate);

            if (subject == null)
            {
                ModelState.AddModelError("SubjectId", "Subject not found or you don't have permission.");
                await LoadCourseDropdownsAsync(teacherIdForUpdate);
                return View("Courses/Edit", model);
            }

            // Update course
            course.Title = model.Title;
            course.Description = model.Description;
            course.Level = model.Level;
            course.SubjectId = model.SubjectId;
            course.IsPublished = model.IsPublished;
            course.UpdatedAt = DateTime.UtcNow;

            // Handle thumbnail - Priority: File > URL > Existing
            if (model.ThumbnailFile != null && model.ThumbnailFile.Length > 0)
            {
                course.ThumbnailUrl = await SaveUploadedFile(model.ThumbnailFile, "course-images");
            }
            else if (!string.IsNullOrEmpty(model.ThumbnailUrl))
            {
                course.ThumbnailUrl = model.ThumbnailUrl;
            }
            // Keep existing if no new file or URL provided
            else if (string.IsNullOrEmpty(course.ThumbnailUrl))
            {
                course.ThumbnailUrl = "/images/default-course.jpg";
            }

            _context.Courses.Update(course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Course updated successfully!";
            return RedirectToAction(nameof(Courses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var teacherId = _userManager.GetUserId(User);

            var course = await _context.Courses
                .Include(c => c.Subject)
                .FirstOrDefaultAsync(c => c.Id == id && c.Subject.TeacherId == teacherId);

            if (course == null)
                return NotFound();

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Course deleted successfully!";
            return RedirectToAction(nameof(Courses));
        }

        // ========== LESSONS ==========
        public async Task<IActionResult> Lessons(int? courseId)
        {
            var teacherId = _userManager.GetUserId(User);

            var lessonsQuery = _context.Lessons
                .Include(l => l.Course)
                .ThenInclude(c => c.Subject)
                .Where(l => l.Course.Subject.TeacherId == teacherId);

            if (courseId.HasValue)
            {
                lessonsQuery = lessonsQuery.Where(l => l.CourseId == courseId);
                ViewBag.CourseName = await _context.Courses
                    .Where(c => c.Id == courseId)
                    .Select(c => c.Title)
                    .FirstOrDefaultAsync();
            }

            var lessons = await lessonsQuery.OrderBy(l => l.Order).ToListAsync();
            ViewBag.CourseId = courseId;

            // Get teacher's courses for filter
            ViewBag.Courses = await _context.Courses
                .Include(c => c.Subject)
                .Where(c => c.Subject.TeacherId == teacherId)
                .ToListAsync();

            return View("Lessons/Index", lessons);
        }

        [HttpGet]
        public async Task<IActionResult> CreateLesson(int? courseId)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                // Get teacher's courses
                var courses = await _context.Courses
                    .Include(c => c.Subject)
                    .Where(c => c.Subject.TeacherId == teacherId)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = $"{c.Title} ({c.Subject.Name})"
                    })
                    .ToListAsync();

                // Validate - teacher must have courses
                if (!courses.Any())
                {
                    TempData["ErrorMessage"] = "You need to create a course first before adding lessons.";
                    return RedirectToAction(nameof(Courses));
                }

                // تحديد courseId الافتراضي
                int selectedCourseId;
                if (courseId.HasValue && courses.Any(c => c.Value == courseId.Value.ToString()))
                {
                    selectedCourseId = courseId.Value;
                }
                else
                {
                    selectedCourseId = Convert.ToInt32(courses.First().Value);
                }

                // حساب الرقم التالي
                var maxOrder = await _context.Lessons
                    .Where(l => l.CourseId == selectedCourseId)
                    .MaxAsync(l => (int?)l.Order);
                var nextOrder = (maxOrder ?? 0) + 1;

                // Always initialize the model
                var model = new CreateLessonViewModel
                {
                    CourseId = selectedCourseId,
                    DurationMinutes = 30, // Sensible default
                    Order = nextOrder,
                    IsEditMode = false
                };

                ViewBag.Courses = courses;

                return View("Lessons/Create", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateLesson GET");
                TempData["ErrorMessage"] = "Error loading lesson form";
                return RedirectToAction(nameof(Lessons));
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetNextOrderNumber(int courseId)
        {
            try
            {
                var maxOrder = await _context.Lessons
                    .Where(l => l.CourseId == courseId)
                    .MaxAsync(l => (int?)l.Order);

                var nextOrder = (maxOrder ?? 0) + 1;
                return Json(new { order = nextOrder });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next order number");
                return Json(new { order = 1 });
            }
        }
        // في TeacherController.cs

        // دالة لإنشاء درس مع رفع فيديو
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(2147483648)]
        public async Task<IActionResult> CreateLesson(CreateLessonViewModel model)
        {
            try
            {
                _logger.LogInformation("CreateLesson POST: Starting with model");

                // إصلاح ModelState
                FixModelStateForCreate(model);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState invalid");

                    // Log detailed errors
                    foreach (var key in ModelState.Keys)
                    {
                        var errors = ModelState[key].Errors;
                        if (errors.Any())
                        {
                            _logger.LogWarning($"Field: {key}, Errors: {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
                        }
                    }

                    var teacherId = _userManager.GetUserId(User);

                    // Reload courses for dropdown
                    var courses = await _context.Courses
                        .Include(c => c.Subject)
                        .Where(c => c.Subject.TeacherId == teacherId)
                        .Select(c => new SelectListItem
                        {
                            Value = c.Id.ToString(),
                            Text = $"{c.Title} ({c.Subject.Name})",
                            Selected = c.Id == model.CourseId
                        })
                        .ToListAsync();

                    ViewBag.Courses = courses;
                    return View("Lessons/Create", model);
                }

                var teacherIdForValidation = _userManager.GetUserId(User);

                // التحقق من صلاحية الكورس
                var course = await _context.Courses
                    .Include(c => c.Subject)
                    .FirstOrDefaultAsync(c => c.Id == model.CourseId && c.Subject.TeacherId == teacherIdForValidation);

                if (course == null)
                {
                    _logger.LogWarning("Course not found: {CourseId} for teacher {TeacherId}",
                        model.CourseId, teacherIdForValidation);
                    ModelState.AddModelError("", "Course not found or access denied.");

                    // Reload courses
                    var courses = await LoadCoursesAsync(teacherIdForValidation, model.CourseId);
                    ViewBag.Courses = courses;

                    return View("Lessons/Create", model);
                }

                // التحقق من الترتيب
                if (await _context.Lessons.AnyAsync(l => l.CourseId == model.CourseId && l.Order == model.Order))
                {
                    var maxOrder = await _context.Lessons
                        .Where(l => l.CourseId == model.CourseId)
                        .MaxAsync(l => (int?)l.Order) ?? 0;
                    model.Order = maxOrder + 1;
                    _logger.LogInformation("Auto-adjusted order to {Order}", model.Order);
                }

                var lesson = new Lesson
                {
                    Title = model.Title?.Trim() ?? "",
                    Description = model.Description?.Trim() ?? "",
                    DurationMinutes = model.DurationMinutes,
                    Order = model.Order,
                    CourseId = model.CourseId,
                    CreatedAt = DateTime.UtcNow,
                    VideoUrl = model.VideoUrl?.Trim()
                };

                // معالجة رفع ملف PDF
                if (model.PdfFile != null && model.PdfFile.Length > 0)
                {
                    _logger.LogInformation("Processing PDF upload: {FileName}", model.PdfFile.FileName);

                    // التحقق من حجم الملف (50MB كحد أقصى)
                    if (model.PdfFile.Length > 50 * 1024 * 1024)
                    {
                        ModelState.AddModelError("PdfFile", "PDF file size exceeds 50MB limit");

                        // Reload courses
                        var courses = await LoadCoursesAsync(teacherIdForValidation, model.CourseId);
                        ViewBag.Courses = courses;

                        return View("Lessons/Create", model);
                    }

                    lesson.PdfUrl = await SaveUploadedFile(model.PdfFile, "lesson-pdfs");
                }

                // حفظ الدرس
                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Lesson created successfully: ID={LessonId}, Title={Title}",
                    lesson.Id, lesson.Title);

                TempData["SuccessMessage"] = $"Lesson '{lesson.Title}' created successfully!";
                return RedirectToAction(nameof(Lessons), new { courseId = model.CourseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateLesson POST");

                var teacherId = _userManager.GetUserId(User);
                var courses = await LoadCoursesAsync(teacherId, model?.CourseId ?? 0);
                ViewBag.Courses = courses;

                TempData["ErrorMessage"] = $"An error occurred while creating the lesson: {ex.Message}";
                return View("Lessons/Create", model);
            }
        }

        // دالة مساعدة لإصلاح ModelState
        private void FixModelStateForCreate(CreateLessonViewModel model)
        {
            // تجاهل PdfUrl في Create لأنه ليس مطلوباً
            ModelState.Remove("PdfUrl");

            // تجاهل Id في Create
            ModelState.Remove("Id");

            // تجاهل الفيديو إذا تم رفع ملف
            if (model.PdfFile != null && model.PdfFile.Length > 0)
            {
                ModelState.Remove("PdfFile");
            }

            // إعادة التحقق من الحقول المطلوبة
            if (string.IsNullOrEmpty(model.Title))
            {
                ModelState.AddModelError("Title", "Lesson title is required");
            }

            if (model.CourseId <= 0)
            {
                ModelState.AddModelError("CourseId", "Please select a course");
            }

            if (model.DurationMinutes <= 0)
            {
                ModelState.AddModelError("DurationMinutes", "Duration must be greater than 0");
            }

            if (model.Order <= 0)
            {
                ModelState.AddModelError("Order", "Order must be greater than 0");
            }

            // التحقق من الفيديو
            if (string.IsNullOrEmpty(model.VideoUrl))
            {
                ModelState.AddModelError("VideoUrl", "Please provide a video URL or upload a video file");
            }
        }
        private async Task<List<SelectListItem>> LoadCoursesAsync(string teacherId, int selectedCourseId)
        {
            var query = _context.Courses
                .Include(c => c.Subject)
                .Where(c => c.Subject.TeacherId == teacherId);

            var courses = await query
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Title} ({c.Subject.Name})",
                    Selected = c.Id == selectedCourseId
                })
                .ToListAsync();

            return courses;
        }
        private async Task<string> UploadVideoFile(IFormFile videoFile)
        {
            if (videoFile == null || videoFile.Length == 0)
                return null;

            // التحقق من نوع الملف
            var allowedExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm", ".wmv" };
            var extension = Path.GetExtension(videoFile.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                throw new Exception("Invalid video format. Allowed formats: MP4, MOV, AVI, MKV, WEBM, WMV");

            // التحقق من الحجم (2GB كحد أقصى)
            if (videoFile.Length > 2147483648)
                throw new Exception("Video file is too large. Maximum size is 2GB");

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "videos");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await videoFile.CopyToAsync(stream);
            }

            return $"/uploads/videos/{uniqueFileName}";
        }

        // دالة تحميل القوائم المنسدلة
        private async Task LoadLessonDropdownsAsync()
        {
            var teacherId = _userManager.GetUserId(User);

            var courses = await _context.Courses
                .Include(c => c.Subject)
                .Where(c => c.Subject.TeacherId == teacherId)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Title} ({c.Subject.Name})"
                })
                .ToListAsync();

            ViewBag.Courses = courses;
        }
        [HttpGet]
        public async Task<IActionResult> EditLesson(int id)
        {
            var teacherId = _userManager.GetUserId(User);

            // Load lesson with course and subject
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                    .ThenInclude(c => c.Subject)
                .FirstOrDefaultAsync(l => l.Id == id && l.Course.Subject.TeacherId == teacherId);

            if (lesson == null)
            {
                TempData["ErrorMessage"] = "Lesson not found or you don't have permission.";
                return RedirectToAction(nameof(Lessons));
            }

            var model = new CreateLessonViewModel
            {
                Id = lesson.Id,
                Title = lesson.Title,
                Description = lesson.Description,
                DurationMinutes = lesson.DurationMinutes,
                Order = lesson.Order,
                CourseId = lesson.CourseId,
                VideoUrl = lesson.VideoUrl,
                PdfUrl = lesson.PdfUrl,
                IsEditMode = true
            };

            // Get teacher's courses for dropdown
            var teacherCourses = await _context.Courses
                .Include(c => c.Subject)
                .Where(c => c.Subject.TeacherId == teacherId)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Title} ({c.Subject.Name})",
                    Selected = c.Id == lesson.CourseId
                })
                .ToListAsync();

            ViewBag.Courses = teacherCourses;
            ViewBag.IsEditMode = true;

            return View("Lessons/Edit", model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(int id, CreateLessonViewModel model)
        {
            try
            {
                model.IsEditMode = true;

                // في Edit، نتجاهل بعض الحقول
                ModelState.Remove("PdfUrl");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("EditLesson: ModelState invalid");

                    // إعادة تحميل القائمة المنسدلة
                    var teacherId = _userManager.GetUserId(User);
                    var teacherCourses = await _context.Courses
                        .Include(c => c.Subject)
                        .Where(c => c.Subject.TeacherId == teacherId)
                        .Select(c => new SelectListItem
                        {
                            Value = c.Id.ToString(),
                            Text = $"{c.Title} ({c.Subject.Name})",
                            Selected = c.Id == model.CourseId
                        })
                        .ToListAsync();

                    ViewBag.Courses = teacherCourses;
                    ViewBag.IsEditMode = true;
                    return View("Lessons/Edit", model);
                }

                var teacherIdForUpdate = _userManager.GetUserId(User);

                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                        .ThenInclude(c => c.Subject)
                    .FirstOrDefaultAsync(l => l.Id == id && l.Course.Subject.TeacherId == teacherIdForUpdate);

                if (lesson == null)
                {
                    TempData["ErrorMessage"] = "Lesson not found or you don't have permission.";
                    return RedirectToAction(nameof(Lessons));
                }

                // تحديث البيانات
                lesson.Title = model.Title;
                lesson.Description = model.Description;
                lesson.DurationMinutes = model.DurationMinutes;
                lesson.Order = model.Order;
                lesson.CourseId = model.CourseId;
                lesson.UpdatedAt = DateTime.UtcNow;

                // تحديث الفيديو
                if (!string.IsNullOrEmpty(model.VideoUrl))
                {
                    lesson.VideoUrl = model.VideoUrl;
                }

                // تحديث PDF
                if (model.PdfFile != null && model.PdfFile.Length > 0)
                {
                    lesson.PdfUrl = await SaveUploadedFile(model.PdfFile, "lesson-pdfs");
                }
                // إذا كان PdfUrl فارغاً ولم يتم رفع ملف جديد، نتركه كما هو

                _context.Lessons.Update(lesson);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Lesson updated successfully!";
                return RedirectToAction(nameof(Lessons), new { courseId = model.CourseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EditLesson");
                TempData["ErrorMessage"] = "An error occurred while updating the lesson.";

                // إعادة تحميل البيانات
                var teacherId = _userManager.GetUserId(User);
                var teacherCourses = await _context.Courses
                    .Include(c => c.Subject)
                    .Where(c => c.Subject.TeacherId == teacherId)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = $"{c.Title} ({c.Subject.Name})",
                        Selected = c.Id == model.CourseId
                    })
                    .ToListAsync();

                ViewBag.Courses = teacherCourses;
                ViewBag.IsEditMode = true;
                return View("Lessons/Edit", model);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var teacherId = _userManager.GetUserId(User);

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .ThenInclude(c => c.Subject)
                .FirstOrDefaultAsync(l => l.Id == id && l.Course.Subject.TeacherId == teacherId);

            if (lesson == null)
                return NotFound();

            var courseId = lesson.CourseId;

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            // Reorder remaining lessons
            var remainingLessons = await _context.Lessons
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.Order)
                .ToListAsync();

            for (int i = 0; i < remainingLessons.Count; i++)
            {
                remainingLessons[i].Order = i + 1;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Lesson deleted successfully!";
            return RedirectToAction(nameof(Lessons), new { courseId = courseId });
        }

        // ========== STUDENTS ==========
        public async Task<IActionResult> Students(int? courseId)
        {
            var teacherId = _userManager.GetUserId(User);

            var enrollmentsQuery = _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .ThenInclude(c => c.Subject)
                .Where(e => e.Course.Subject.TeacherId == teacherId);

            if (courseId.HasValue)
            {
                enrollmentsQuery = enrollmentsQuery.Where(e => e.CourseId == courseId);
                ViewBag.CourseTitle = await _context.Courses
                    .Where(c => c.Id == courseId)
                    .Select(c => c.Title)
                    .FirstOrDefaultAsync();
            }

            var enrollments = await enrollmentsQuery
                .OrderByDescending(e => e.EnrolledAt)
                .ToListAsync();

            ViewBag.CourseId = courseId;
            ViewBag.Courses = await _context.Courses
                .Include(c => c.Subject)
                .Where(c => c.Subject.TeacherId == teacherId)
                .ToListAsync();

            return View("Students/Index", enrollments);
        }

        // In TeacherController.cs - Add these methods

        // ========== GET COURSE STATS ==========
        // ========== GET RECENT ACTIVITY ==========

        // ========== GET TOP PERFORMING COURSES ==========
        // في TeacherController.cs - أضف هذا الإجراء الجديد
        [HttpGet]
        public async Task<IActionResult> LessonDetails(int id)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);
                var teacher = await _userManager.GetUserAsync(User);

                // الحصول على الدرس
                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                        .ThenInclude(c => c.Subject)
                    .Include(l => l.Course)
                        .ThenInclude(c => c.Lessons.OrderBy(ls => ls.Order))
                    .FirstOrDefaultAsync(l => l.Id == id && l.Course.Subject.TeacherId == teacherId);

                if (lesson == null)
                {
                    TempData["ErrorMessage"] = "Lesson not found or you don't have access";
                    return RedirectToAction(nameof(Lessons));
                }

                // الحصول على الطلاب المسجلين
                var enrolledStudents = await _context.Enrollments
                    .Include(e => e.Student)
                    .Where(e => e.CourseId == lesson.CourseId)
                    .Select(e => e.Student)
                    .ToListAsync();

                // الحصول على التعليقات بطريقة بسيطة
                var comments = await _context.Comments
                    .Include(c => c.User)
                    .Where(c => c.LessonId == lesson.Id && !c.IsDeleted && c.ParentCommentId == null)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                // تحضير الردود
                var commentViewModels = new List<TeacherCommentViewModel>();
                foreach (var comment in comments)
                {
                    var replies = await _context.Comments
                        .Include(c => c.User)
                        .Where(c => c.ParentCommentId == comment.Id && !c.IsDeleted)
                        .OrderBy(c => c.CreatedAt)
                        .ToListAsync();

                    var reactions = await _context.CommentReactions
                        .Where(cr => cr.CommentId == comment.Id)
                        .GroupBy(cr => cr.ReactionType)
                        .Select(g => new { ReactionType = g.Key, Count = g.Count() })
                        .ToListAsync();

                    var commentVm = new TeacherCommentViewModel
                    {
                        Id = comment.Id,
                        Content = comment.Content,
                        UserId = comment.UserId,
                        UserName = comment.User?.FullName ?? "Unknown",
                        UserProfileImage = comment.User?.ProfileImageUrl ?? "/images/avatar.png",
                        IsTeacher = comment.User?.IsTeacher ?? false,
                        CreatedAt = comment.CreatedAt,
                        TimeAgo = GetTimeAgo(comment.CreatedAt),
                        IsEdited = comment.IsEdited,
                        CanDelete = comment.UserId == teacherId || comment.User?.IsTeacher == false,
                        CanEdit = comment.UserId == teacherId
                    };

                    // إضافة الردود
                    foreach (var reply in replies)
                    {
                        commentVm.Replies.Add(new TeacherReplyViewModel
                        {
                            Id = reply.Id,
                            Content = reply.Content,
                            UserId = reply.UserId,
                            UserName = reply.User?.FullName ?? "Unknown",
                            UserProfileImage = reply.User?.ProfileImageUrl ?? "/images/avatar.png",
                            CreatedAt = reply.CreatedAt,
                            TimeAgo = GetTimeAgo(reply.CreatedAt),
                            IsEdited = reply.IsEdited
                        });
                    }

                    // إضافة التفاعلات
                    foreach (var reaction in reactions)
                    {
                        commentVm.Reactions[reaction.ReactionType] = reaction.Count;
                    }

                    commentViewModels.Add(commentVm);
                }

                // إعداد النموذج
                var model = new TeacherLessonViewModel
                {
                    Lesson = lesson,
                    Course = lesson.Course,
                    TotalStudents = enrolledStudents.Count,
                    EnrolledStudents = enrolledStudents,
                    CommentsCount = comments.Count,
                    Comments = commentViewModels,
                    HasPdf = !string.IsNullOrEmpty(lesson.PdfUrl),
                    CourseLessons = lesson.Course.Lessons?.ToList() ?? new List<Lesson>(),
                    CurrentUserId = teacherId
                };

                return View("Lessons/Details", model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR LessonDetails: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading lesson";
                return RedirectToAction(nameof(Lessons));
            }
        }
        private async Task<List<TeacherCommentViewModel>> GetTeacherComments(int lessonId)
        {
            try
            {
                var comments = await _context.Comments
                    .Include(c => c.User)
                    .Where(c => c.LessonId == lessonId && !c.IsDeleted && c.ParentCommentId == null)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        Comment = c,
                        User = c.User,
                        Replies = c.Replies.Where(r => !r.IsDeleted).ToList(),
                        Reactions = _context.CommentReactions
                            .Where(cr => cr.CommentId == c.Id)
                            .GroupBy(cr => cr.ReactionType)
                            .Select(g => new
                            {
                                ReactionType = g.Key,
                                Count = g.Count()
                            })
                            .ToList()
                    })
                    .ToListAsync();

                var result = new List<TeacherCommentViewModel>();

                foreach (var item in comments)
                {
                    var commentVm = new TeacherCommentViewModel
                    {
                        Id = item.Comment.Id,
                        Content = item.Comment.Content,
                        UserId = item.Comment.UserId,
                        UserName = item.User?.FullName ?? "Unknown User",
                        UserProfileImage = item.User?.ProfileImageUrl ?? "/images/avatar.png",
                        IsTeacher = item.User?.IsTeacher ?? false,
                        CreatedAt = item.Comment.CreatedAt,
                        TimeAgo = GetTimeAgo(item.Comment.CreatedAt),
                        IsEdited = item.Comment.IsEdited
                    };

                    // Add replies
                    if (item.Replies != null && item.Replies.Any())
                    {
                        foreach (var reply in item.Replies)
                        {
                            var replyUser = await _context.Users.FindAsync(reply.UserId);
                            commentVm.Replies.Add(new TeacherReplyViewModel
                            {
                                Id = reply.Id,
                                Content = reply.Content,
                                UserId = reply.UserId,
                                UserName = replyUser?.FullName ?? "Unknown User",
                                UserProfileImage = replyUser?.ProfileImageUrl ?? "/images/avatar.png",
                                CreatedAt = reply.CreatedAt,
                                TimeAgo = GetTimeAgo(reply.CreatedAt),
                                IsEdited = reply.IsEdited
                            });
                        }
                    }

                    // Add reactions as dictionary
                    if (item.Reactions != null && item.Reactions.Any())
                    {
                        commentVm.Reactions = item.Reactions
                            .ToDictionary(r => r.ReactionType, r => r.Count);
                    }

                    result.Add(commentVm);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teacher comments");
                return new List<TeacherCommentViewModel>();
            }
        }
        // دالة مساعدة للحصول على ملاحظات المعلم
        private async Task<TeacherNotesViewModel> GetTeacherNotes(int lessonId, string teacherId)
        {
            var note = await _context.LessonNotes
                .FirstOrDefaultAsync(n => n.LessonId == lessonId && n.StudentId == teacherId);

            if (note == null)
            {
                return new TeacherNotesViewModel
                {
                    LessonId = lessonId,
                    TeacherId = teacherId,
                    Content = "",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastUpdated = "Not saved yet"
                };
            }

            return new TeacherNotesViewModel
            {
                Id = note.Id,
                LessonId = note.LessonId,
                TeacherId = note.StudentId,
                Content = note.Content,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt,
                LastUpdated = GetTimeAgo(note.UpdatedAt)
            };
        }
        [HttpPost]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacherComment([FromForm] int lessonId, [FromForm] string content)
        {
            try
            {
                Console.WriteLine($"Add Teacher Comment - Lesson: {lessonId}, Content: {content}");

                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "Comment content is required" });
                }

                var teacherId = _userManager.GetUserId(User);
                var teacher = await _userManager.GetUserAsync(User);

                if (string.IsNullOrEmpty(teacherId))
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // التحقق من أن الدرس للمعلم
                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                    .ThenInclude(c => c.Subject)
                    .FirstOrDefaultAsync(l => l.Id == lessonId && l.Course.Subject.TeacherId == teacherId);

                if (lesson == null)
                {
                    return Json(new { success = false, message = "Lesson not found or unauthorized" });
                }

                // إنشاء التعليق
                var comment = new Comment
                {
                    Content = content.Trim(),
                    UserId = teacherId,
                    LessonId = lessonId,
                    CreatedAt = DateTime.UtcNow,
                    IsEdited = false,
                    IsDeleted = false
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Comment created: ID={comment.Id}");

                return Json(new
                {
                    success = true,
                    commentId = comment.Id,
                    message = "Comment added successfully",
                    teacherName = teacher?.FullName,
                    teacherImage = teacher?.ProfileImageUrl ?? "/images/avatar.png",
                    timeAgo = "just now",
                    content = comment.Content
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR AddTeacherComment: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTeacherLessonComments(int lessonId)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                // التحقق من الوصول
                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                    .ThenInclude(c => c.Subject)
                    .FirstOrDefaultAsync(l => l.Id == lessonId && l.Course.Subject.TeacherId == teacherId);

                if (lesson == null)
                {
                    return Json(new { success = false, message = "Unauthorized access" });
                }

                var comments = await GetTeacherComments(lessonId);

                return Json(new
                {
                    success = true,
                    comments = comments,
                    totalComments = comments.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teacher comments");
                return Json(new { success = false, message = "Error loading comments" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeacherComment(int id)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                var comment = await _context.Comments
                    .Include(c => c.Replies)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comment == null)
                {
                    return Json(new { success = false, message = "Comment not found" });
                }

                // تحقق أن المعلم هو صاحب التعليق
                if (comment.UserId != teacherId)
                {
                    // أو إذا كان المعلم صاحب الدرس
                    var lesson = await _context.Lessons
                        .Include(l => l.Course)
                        .ThenInclude(c => c.Subject)
                        .FirstOrDefaultAsync(l => l.Id == comment.LessonId);

                    if (lesson?.Course?.Subject?.TeacherId != teacherId)
                    {
                        return Json(new { success = false, message = "Unauthorized" });
                    }
                }

                // إذا كان هناك ردود، نضع محتوى "تم الحذف"
                if (comment.Replies != null && comment.Replies.Any())
                {
                    comment.IsDeleted = true;
                    comment.Content = "[Comment deleted by teacher]";
                    _context.Comments.Update(comment);
                }
                else
                {
                    _context.Comments.Remove(comment);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Comment deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR DeleteTeacherComment: {ex.Message}");
                return Json(new { success = false, message = "Error deleting comment" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacherReply(int commentId, string content)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                var comment = await _context.Comments.FindAsync(commentId);
                if (comment == null)
                    return Json(new { success = false, message = "Comment not found" });

                var reply = new Comment
                {
                    Content = content.Trim(),
                    UserId = teacherId,
                    LessonId = comment.LessonId,
                    ParentCommentId = commentId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Comments.Add(reply);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Reply added" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR AddTeacherReply: {ex.Message}");
                return Json(new { success = false, message = "Error adding reply" });
            }
        }

    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SaveTeacherNotes(int lessonId, string notes)
{
    try
    {
        var teacherId = _userManager.GetUserId(User);
        
        var note = await _context.LessonNotes
            .FirstOrDefaultAsync(n => n.LessonId == lessonId && n.StudentId == teacherId);

        if (note != null)
        {
            note.Content = notes?.Trim();
            note.UpdatedAt = DateTime.UtcNow;
            _context.LessonNotes.Update(note);
        }
        else
        {
            note = new LessonNote
            {
                LessonId = lessonId,
                StudentId = teacherId,
                Content = notes?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.LessonNotes.Add(note);
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Notes saved" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR SaveTeacherNotes: {ex.Message}");
        return Json(new { success = false, message = "Error saving notes" });
    }
}
        [HttpGet]
        public async Task<IActionResult> GetTeacherNotes(int lessonId)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                var note = await _context.LessonNotes
                    .FirstOrDefaultAsync(n => n.LessonId == lessonId && n.StudentId == teacherId);

                if (note == null)
                {
                    return Json(new { success = true, notes = "", hasNotes = false });
                }

                return Json(new
                {
                    success = true,
                    notes = note.Content,
                    hasNotes = !string.IsNullOrEmpty(note.Content),
                    lastUpdated = GetTimeAgo(note.UpdatedAt)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teacher notes");
                return Json(new { success = false, message = "Error loading notes" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacherReaction(int commentId, string reactionType)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                var existingReaction = await _context.CommentReactions
                    .FirstOrDefaultAsync(cr => cr.CommentId == commentId && cr.UserId == teacherId);

                if (existingReaction != null)
                {
                    if (existingReaction.ReactionType == reactionType)
                    {
                        _context.CommentReactions.Remove(existingReaction);
                    }
                    else
                    {
                        existingReaction.ReactionType = reactionType;
                        existingReaction.CreatedAt = DateTime.UtcNow;
                        _context.CommentReactions.Update(existingReaction);
                    }
                }
                else
                {
                    var reaction = new CommentReaction
                    {
                        UserId = teacherId,
                        CommentId = commentId,
                        ReactionType = reactionType,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CommentReactions.Add(reaction);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Reaction updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding teacher reaction");
                return Json(new { success = false, message = "Error adding reaction" });
            }
        }
private async Task SendCommentNotificationToStudents(int lessonId, string teacherName)
{
    var lesson = await _context.Lessons
        .Include(l => l.Course)
        .ThenInclude(c => c.Enrollments)
        .FirstOrDefaultAsync(l => l.Id == lessonId);

    if (lesson?.Course?.Enrollments != null)
    {
        foreach (var enrollment in lesson.Course.Enrollments)
        {
            var notification = new Notification
            {
                UserId = enrollment.StudentId,
                Type = "teacher_comment",
                Title = "New Comment from Teacher",
                Message = $"{teacherName} commented on lesson: {lesson.Title}",
                RelatedId = lessonId,
                RelatedType = "lesson",
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync();
    }
}

private async Task SendReplyNotification(string userId, string teacherId)
{
    var teacher = await _userManager.FindByIdAsync(teacherId);

    var notification = new Notification
    {
        UserId = userId,
        Type = "teacher_reply",
        Title = "Teacher Replied",
        Message = $"{teacher?.FullName} replied to your comment",
        CreatedAt = DateTime.UtcNow
    };

    _context.Notifications.Add(notification);
    await _context.SaveChangesAsync();
}
        // أضف هذه الدوال في TeacherController
        [HttpGet]
        public async Task<JsonResult> GetUnreadMessagesCount()
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                var unreadCount = await _context.ChatMessages
                    .CountAsync(m =>
                        (m.Chat.User1Id == teacherId || m.Chat.User2Id == teacherId) &&
                        m.SenderId != teacherId &&
                        !m.IsRead);

                return Json(new { success = true, count = unreadCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread messages count");
                return Json(new { success = false, count = 0 });
            }
        }

        [HttpPost]
        public async Task<IActionResult> StartChatWithStudent(string studentId, string initialMessage = null)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);
                var teacher = await _userManager.GetUserAsync(User);

                // Check if student exists
                var student = await _userManager.FindByIdAsync(studentId);
                if (student == null || student.IsTeacher)
                {
                    return Json(new { success = false, message = "Student not found" });
                }

                // Check for existing chat
                var existingChat = await _context.Chats
                    .FirstOrDefaultAsync(c =>
                        (c.User1Id == teacherId && c.User2Id == studentId) ||
                        (c.User1Id == studentId && c.User2Id == teacherId));

                if (existingChat != null)
                {
                    // Add message if provided
                    if (!string.IsNullOrEmpty(initialMessage))
                    {
                        var message = new ChatMessage
                        {
                            ChatId = existingChat.Id,
                            SenderId = teacherId,
                            Content = initialMessage,
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false
                        };

                        _context.ChatMessages.Add(message);
                        existingChat.LastMessageAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }

                    return Json(new
                    {
                        success = true,
                        chatId = existingChat.Id,
                        redirect = Url.Action("Details", "Chat", new { id = existingChat.Id })
                    });
                }

                // Create new chat
                var chat = new Chat
                {
                    User1Id = teacherId,
                    User2Id = studentId,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = null
                };

                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();

                // Add initial message
                if (!string.IsNullOrEmpty(initialMessage))
                {
                    var message = new ChatMessage
                    {
                        ChatId = chat.Id,
                        SenderId = teacherId,
                        Content = initialMessage,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.ChatMessages.Add(message);
                    chat.LastMessageAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    success = true,
                    chatId = chat.Id,
                    redirect = Url.Action("Lessons/Details", "Teacher", new { id = chat.Id })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting chat with student");
                return Json(new { success = false, message = "Error starting chat" });
            }
        }
        #region teacher chats
        public async Task<IActionResult> TeacherChat()
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);

                // Get all chats for teacher
                var chats = await _context.Chats
                    .Include(c => c.User1)
                    .Include(c => c.User2)
                    .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                    .Where(c => c.User1Id == teacherId || c.User2Id == teacherId)
                    .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                    .ToListAsync();

                var chatViewModels = new List<TeacherChatViewModel>();
                foreach (var chat in chats)
                {
                    var otherUser = chat.User1Id == teacherId ? chat.User2 : chat.User1;
                    var lastMessage = chat.Messages?.FirstOrDefault();
                    var unreadCount = await _context.ChatMessages
                        .CountAsync(m => m.ChatId == chat.Id &&
                                       m.SenderId != teacherId &&
                                       !m.IsRead);

                    chatViewModels.Add(new TeacherChatViewModel
                    {
                        Id = chat.Id,
                        OtherUserId = otherUser.Id,
                        OtherUserName = otherUser.FullName,
                        OtherUserProfileImage = otherUser.ProfileImageUrl ?? "/images/avatar.png",
                        IsTeacher = otherUser.IsTeacher,
                        LastMessage = lastMessage?.Content,
                        LastMessageAt = lastMessage?.CreatedAt,
                        UnreadCount = unreadCount,
                        IsOnline = ChatHub.IsUserOnline(otherUser.Id)
                    });
                }

                return View("Chat/Index", chatViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading teacher chats");
                return View("Chat/Index", new List<TeacherChatViewModel>());
            }
        }
        // GET: /Teacher/Chat/Details/{id}
        public async Task<IActionResult> TeacherChatDetails(int id)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);

                // الحصول على المحادثة
                var chat = await _context.Chats
                    .Include(c => c.User1)
                    .Include(c => c.User2)
                    .FirstOrDefaultAsync(c => c.Id == id &&
                        (c.User1Id == teacherId || c.User2Id == teacherId));

                if (chat == null)
                {
                    TempData["ErrorMessage"] = "Chat not found";
                    return RedirectToAction(nameof(TeacherChat));
                }

                var otherUser = chat.User1Id == teacherId ? chat.User2 : chat.User1;

                // الحصول على الرسائل (آخر 50 رسالة)
                var messages = await _context.ChatMessages
                    .Where(m => m.ChatId == id)
                    .Include(m => m.Sender)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(50)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new TeacherChatMessageViewModel
                    {
                        Id = m.Id,
                        ChatId = m.ChatId,
                        Content = m.Content,
                        SenderId = m.SenderId,
                        SenderName = m.Sender.FullName,
                        SenderProfileImage = m.Sender.ProfileImageUrl ?? "/images/avatar.png",
                        IsOwnMessage = m.SenderId == teacherId,
                        IsRead = m.IsRead,
                        CreatedAt = m.CreatedAt,
                        TimeAgo = GetTimeAgo(m.CreatedAt)
                    })
                    .ToListAsync();

                // تحديث الرسائل غير المقروءة
                var unreadMessages = await _context.ChatMessages
                    .Where(m => m.ChatId == id &&
                               m.SenderId != teacherId &&
                               !m.IsRead)
                    .ToListAsync();

                if (unreadMessages.Any())
                {
                    foreach (var msg in unreadMessages)
                    {
                        msg.IsRead = true;
                        msg.ReadAt = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                }

                var model = new TeacherChatDetailsViewModel
                {
                    Chat = new TeacherChatViewModel
                    {
                        Id = chat.Id,
                        OtherUserId = otherUser.Id,
                        OtherUserName = otherUser.FullName,
                        OtherUserProfileImage = otherUser.ProfileImageUrl ?? "/images/avatar.png",
                        IsTeacher = otherUser.IsTeacher,
                        IsOnline = ChatHub.IsUserOnline(otherUser.Id),
                        LastMessage = messages.LastOrDefault()?.Content,
                        LastMessageAt = messages.LastOrDefault()?.CreatedAt,
                        UnreadCount = 0
                    },
                    Messages = messages,
                    NewMessage = new SendMessageViewModel
                    {
                        ChatId = chat.Id
                    }
                };

                ViewBag.TeacherId = teacherId;
                ViewBag.TeacherName = currentUser.FullName;
                ViewBag.TeacherProfileImage = currentUser.ProfileImageUrl ?? "/images/avatar.png";

                return View("Chat/Details", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chat details");
                TempData["ErrorMessage"] = "Error loading chat";
                return RedirectToAction(nameof(TeacherChat));
            }
        }
        // GET: /Teacher/Chat/Create
        public async Task<IActionResult> TeacherChatCreate(string studentId = null)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                // إذا كان هناك studentId محدد
                if (!string.IsNullOrEmpty(studentId))
                {
                    var student = await _userManager.FindByIdAsync(studentId);
                    if (student != null && !student.IsTeacher)
                    {
                        ViewBag.StudentId = studentId;
                        ViewBag.StudentName = student.FullName;
                        ViewBag.StudentProfileImage = student.ProfileImageUrl ?? "/images/avatar.png";
                    }
                }

                // الحصول على قائمة الطلاب المسجلين في دورات المعلم
                var enrolledStudents = await _context.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Course)
                    .ThenInclude(c => c.Subject)
                    .Where(e => e.Course.Subject.TeacherId == teacherId)
                    .Select(e => e.Student)
                    .Distinct()
                    .ToListAsync();

                ViewBag.EnrolledStudents = enrolledStudents;

                return View("Chat/Create");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chat create page");
                return RedirectToAction(nameof(TeacherChat));
            }
        }

        // POST: /Teacher/Chat/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTeacherMessage(SendMessageViewModel model)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                // التحقق من وجود المحادثة
                var chat = await _context.Chats
                    .FirstOrDefaultAsync(c => c.Id == model.ChatId &&
                        (c.User1Id == teacherId || c.User2Id == teacherId));

                if (chat == null)
                {
                    return Json(new { success = false, message = "Chat not found" });
                }

                var message = new ChatMessage
                {
                    ChatId = model.ChatId,
                    SenderId = teacherId,
                    Content = model.Content?.Trim() ?? "",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    ReadAt = DateTime.MinValue
                };

                _context.ChatMessages.Add(message);
                chat.LastMessageAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Send notification to student
                var studentId = chat.User1Id == teacherId ? chat.User2Id : chat.User1Id;

                var notification = new Notification
                {
                    UserId = studentId,
                    Type = "new_message",
                    Title = "New Message from Teacher",
                    Message = "You have a new message from your teacher",
                    RelatedId = chat.Id,
                    RelatedType = "chat",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    messageId = message.Id,
                    timestamp = message.CreatedAt.ToString("hh:mm tt")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending teacher message");
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // GET: /Teacher/Chat/GetChats (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetTeacherChats()
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                var chats = await _context.Chats
                    .Include(c => c.User1)
                    .Include(c => c.User2)
                    .Where(c => c.User1Id == teacherId || c.User2Id == teacherId)
                    .Select(c => new
                    {
                        Id = c.Id,
                        User1Id = c.User1Id,
                        User2Id = c.User2Id,
                        CreatedAt = c.CreatedAt,
                        LastMessageAt = c.LastMessageAt
                    })
                    .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                var chatViewModels = new List<TeacherChatViewModel>();

                foreach (var chat in chats)
                {
                    var otherUserId = chat.User1Id == teacherId ? chat.User2Id : chat.User1Id;
                    var otherUser = await _userManager.Users
                        .Where(u => u.Id == otherUserId)
                        .Select(u => new
                        {
                            u.Id,
                            u.FullName,
                            ProfileImageUrl = u.ProfileImageUrl ?? "/images/avatar.png",
                            u.IsTeacher
                        })
                        .FirstOrDefaultAsync();

                    if (otherUser == null) continue;

                    var lastMessage = await _context.ChatMessages
                        .Where(m => m.ChatId == chat.Id)
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => new { m.Content, m.CreatedAt })
                        .FirstOrDefaultAsync();

                    var unreadCount = await _context.ChatMessages
                        .CountAsync(m => m.ChatId == chat.Id &&
                                       m.SenderId != teacherId &&
                                       !m.IsRead);

                    chatViewModels.Add(new TeacherChatViewModel
                    {
                        Id = chat.Id,
                        OtherUserId = otherUser.Id,
                        OtherUserName = otherUser.FullName,
                        OtherUserProfileImage = otherUser.ProfileImageUrl,
                        IsTeacher = otherUser.IsTeacher,
                        LastMessage = lastMessage?.Content?.Length > 30
                            ? lastMessage.Content.Substring(0, 30) + "..."
                            : lastMessage?.Content,
                        LastMessageAt = lastMessage?.CreatedAt,
                        UnreadCount = unreadCount,
                        IsOnline = ChatHub.IsUserOnline(otherUser.Id)
                    });
                }

                return Json(new
                {
                    success = true,
                    chats = chatViewModels,
                    currentUserId = teacherId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teacher chats");
                return Json(new { success = false, chats = new List<TeacherChatViewModel>() });
            }
        }

        // GET: /Teacher/Chat/GetChatMessages
        [HttpGet]
        public async Task<IActionResult> GetTeacherChatMessages(int chatId, int skip = 0, int take = 50)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                // التحقق من الوصول
                var canAccess = await _context.Chats
                    .AnyAsync(c => c.Id == chatId &&
                        (c.User1Id == teacherId || c.User2Id == teacherId));

                if (!canAccess)
                {
                    return Json(new { success = false, message = "Access denied" });
                }

                var messages = await _context.ChatMessages
                    .Where(m => m.ChatId == chatId)
                    .Include(m => m.Sender)
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .Select(m => new TeacherChatMessageViewModel
                    {
                        Id = m.Id,
                        ChatId = m.ChatId,
                        Content = m.Content,
                        SenderId = m.SenderId,
                        SenderName = m.Sender.FullName,
                        SenderProfileImage = m.Sender.ProfileImageUrl ?? "/images/avatar.png",
                        IsOwnMessage = m.SenderId == teacherId,
                        IsRead = m.IsRead,
                        CreatedAt = m.CreatedAt,
                        TimeAgo = GetTimeAgo(m.CreatedAt)
                    })
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                return Json(new { success = true, messages = messages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teacher chat messages");
                return Json(new { success = false, message = "Error loading messages" });
            }
        }
        private static string GetTimeAgo(DateTime date)
        {
            var span = DateTime.UtcNow - date;

            if (span.TotalDays >= 365)
                return $"{(int)(span.TotalDays / 365)} years ago";
            if (span.TotalDays >= 30)
                return $"{(int)(span.TotalDays / 30)} months ago";
            if (span.TotalDays >= 7)
                return $"{(int)(span.TotalDays / 7)} weeks ago";
            if (span.TotalDays >= 1)
                return $"{(int)span.TotalDays} days ago";
            if (span.TotalHours >= 1)
                return $"{(int)span.TotalHours} hours ago";
            if (span.TotalMinutes >= 1)
                return $"{(int)span.TotalMinutes} minutes ago";

            return "just now";
        }

        // GET: /Teacher/Chat/GetEnrolledStudents
        [HttpGet]
        public async Task<IActionResult> GetTeacherEnrolledStudents(string search = "")
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                var students = await _context.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Course)
                    .ThenInclude(c => c.Subject)
                    .Where(e => e.Course.Subject.TeacherId == teacherId)
                    .Select(e => e.Student)
                    .Distinct()
                    .Where(s => string.IsNullOrEmpty(search) ||
                               s.FullName.Contains(search) ||
                               s.Email.Contains(search))
                    .Select(s => new
                    {
                        Id = s.Id,
                        Name = s.FullName,
                        Email = s.Email,
                        ProfileImageUrl = s.ProfileImageUrl ?? "/images/avatar.png",
                        EnrolledCourses = _context.Enrollments
                            .Count(e => e.StudentId == s.Id &&
                                       e.Course.Subject.TeacherId == teacherId)
                    })
                    .OrderBy(s => s.Name)
                    .Take(20)
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    students = students
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrolled students");
                return Json(new { success = false, students = new List<object>() });
            }
        }

        // POST: /Teacher/Chat/MarkMessagesAsRead
        [HttpPost]
        public async Task<IActionResult> MarkTeacherMessagesAsRead(int chatId)
        {
            try
            {
                var teacherId = _userManager.GetUserId(User);

                var unreadMessages = await _context.ChatMessages
                    .Where(m => m.ChatId == chatId &&
                               m.SenderId != teacherId &&
                               !m.IsRead)
                    .ToListAsync();

                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                    message.ReadAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking teacher messages as read");
                return Json(new { success = false });
            }
        }
        #endregion
        private async Task<string> SaveUploadedFile(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folder);
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{folder}/{uniqueFileName}";
        }
    }
}