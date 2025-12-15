// Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_Lessons.Models;
using Web_Lessons.ViewModels;

namespace Web_Lessons.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager; // Add this

        public AdminController(AppDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager; // Initialize it

            _context = context;
            _userManager = userManager;
        }

        // In AdminController.cs

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

            return View("Courses/Index", courses); // Views/Teacher/Courses/Index.cshtml
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalTeachers = await _context.Users.CountAsync(u => u.IsTeacher),
                TotalStudents = await _context.Users.CountAsync(u => !u.IsTeacher),
                TotalCourses = await _context.Courses.CountAsync(),
                TotalSubjects = await _context.Subjects.CountAsync(),
                TotalLessons = await _context.Lessons.CountAsync(),
                RecentUsers = await _context.Users
                    .OrderByDescending(u => u.CreatedOn)
                    .Take(10)
                    .ToListAsync()
            };

            return View(model);
        }

        public async Task<IActionResult> Statistics()
        {
            var lastMonth = DateTime.UtcNow.AddMonths(-1);

            var model = new AdminStatisticsViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                UsersThisMonth = await _context.Users
                    .CountAsync(u => u.CreatedOn >= lastMonth),
                TeachersCount = await _context.Users.CountAsync(u => u.IsTeacher),
                StudentsCount = await _context.Users.CountAsync(u => !u.IsTeacher),
                TotalCourses = await _context.Courses.CountAsync(),
                TotalSubjects = await _context.Subjects.CountAsync(),
                TotalLessons = await _context.Lessons.CountAsync(),
                TotalEnrollments = await _context.Enrollments.CountAsync(),
                ActiveUsers = await _context.Users
                    .CountAsync(u => u.LastLogin >= DateTime.UtcNow.AddDays(-30)),
                RecentEnrollments = await _context.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Subject)
                    .OrderByDescending(e => e.EnrolledAt)
                    .Take(10)
                    .ToListAsync(),
                RecentCourses = await _context.Courses
                    .Include(c => c.Subject)
                    .Include(c => c.Lessons)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View("Statistics/Index", model);
        }

        // In AdminController.cs
        [HttpGet]
        public async Task<IActionResult> Users(string role, string search)
        {
            try
            {
                // Get all users
                var usersQuery = _userManager.Users.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    usersQuery = usersQuery.Where(u =>
                        u.FullName.Contains(search) ||
                        u.Email.Contains(search));
                }

                var users = await usersQuery.ToListAsync();

                // Get user roles
                var userRoles = new Dictionary<string, List<string>>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userRoles[user.Id] = roles.ToList();
                }

                // Apply role filter
                if (!string.IsNullOrEmpty(role))
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                    var userIdsInRole = usersInRole.Select(u => u.Id).ToHashSet();
                    users = users.Where(u => userIdsInRole.Contains(u.Id)).ToList();
                }

                // Get all roles for the filter dropdown
                var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

                // Pass data to view
                ViewBag.UserRoles = userRoles;
                ViewBag.Roles = allRoles;
                ViewBag.SelectedRole = role;
                ViewBag.Search = search;

                return View("Users/Index",users); // ✅ Pass the users list as model
            }
            catch (Exception ex)
            {
                // Log error
                TempData["Error"] = "Error loading users: " + ex.Message;
                return View(new List<ApplicationUser>()); // Return empty list on error
            }
        }


        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsTeacher = user.IsTeacher,
                IsActive = !(user.LockoutEnd != null && user.LockoutEnd > DateTime.Now),
                SelectedRoles = userRoles.ToArray()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                    return NotFound();

                // Update basic info
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.UserName = model.Email;
                user.IsTeacher = model.IsTeacher;

                // Update lockout status
                if (!model.IsActive)
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                }
                else
                {
                    await _userManager.SetLockoutEndDateAsync(user, null);
                }

                // Update roles (تأكد أن SelectedRoles ليست null)
                if (model.SelectedRoles != null)
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    var rolesToRemove = currentRoles.Except(model.SelectedRoles);
                    var rolesToAdd = model.SelectedRoles.Except(currentRoles);

                    await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    await _userManager.AddToRolesAsync(user, rolesToAdd);
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "User updated successfully!";
                    return RedirectToAction("Users");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Don't allow self-deletion
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["ErrorMessage"] = "You cannot delete your own account!";
                return RedirectToAction("Users");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete user.";
            }

            return RedirectToAction("Users");
        }

        // In AdminController.cs - Add these actions:

        // GET: Display the Add Teacher form
        // تأكد إن ده موجود في AdminController.cs
        [HttpGet]
        public IActionResult AddTeacher()
        {
            return View("Users/AddTeacher");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacher(AddTeacherViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    return View("Users/AddTeacher", model);
                }

                var teacher = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    IsTeacher = true,
                    CreatedOn = DateTime.UtcNow,
                    EmailConfirmed = true,  // 1. دا أهم حاجة
                    LockoutEnabled = false  // 2. دا تاني أهم حاجة
                };

                var result = await _userManager.CreateAsync(teacher, model.Password);

                if (result.Succeeded)
                {
                    // 3. دا تأكيد
                    await _userManager.SetLockoutEndDateAsync(teacher, null);

                    await _userManager.AddToRoleAsync(teacher, "Teacher");

                    TempData["SuccessMessage"] = $"Teacher created!";
                    return RedirectToAction(nameof(Users));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View("Users/AddTeacher", model);
        }
        [HttpGet]
        public IActionResult Settings()
        {
            // In real app, load from database
            var model = new SystemSettingsViewModel();
            return View("Settings/Index", model);
        }

        [HttpPost]
        public async Task<IActionResult> Settings(SystemSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                // In real app, save to database
                TempData["SuccessMessage"] = "Settings saved successfully!";
                return RedirectToAction("Settings");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Toggle lockout
            if (user.LockoutEnd == null || user.LockoutEnd < DateTime.Now)
            {
                // Lock user
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                TempData["SuccessMessage"] = "User account locked!";
            }
            else
            {
                // Unlock user
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = "User account unlocked!";
            }

            return RedirectToAction("Users");
        }
    }
}
