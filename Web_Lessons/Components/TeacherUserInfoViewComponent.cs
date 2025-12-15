using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Web_Lessons.Models;

namespace Web_Lessons.Components
{
    [ViewComponent(Name = "TeacherUserInfo")]
    public class TeacherUserInfoViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherUserInfoViewComponent(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                // استخدام ViewContext.HttpContext.User بدلاً من User مباشرة
                if (ViewContext.HttpContext?.User == null ||
                    !ViewContext.HttpContext.User.Identity.IsAuthenticated)
                {
                    return Content(string.Empty);
                }

                var user = await _userManager.GetUserAsync(ViewContext.HttpContext.User);
                if (user == null || !user.IsTeacher)
                {
                    return Content(string.Empty);
                }

                var model = new TeacherUserInfoViewModel
                {
                    FullName = user.FullName,
                    ProfileImageUrl = user.ProfileImageUrl ?? "/images/avatar.png",
                    Email = user.Email
                };

                return View(model);
            }
            catch (Exception ex)
            {
                // يمكنك تسجيل الخطأ هنا إذا أردت
                Console.WriteLine($"Error in TeacherUserInfoViewComponent: {ex.Message}");
                return Content(string.Empty);
            }
        }
    }

    public class TeacherUserInfoViewModel
    {
        public string FullName { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Email { get; set; }

        public string FirstName => FullName?.Split(' ')[0] ?? "Teacher";
        public string UserInitial => !string.IsNullOrEmpty(FullName)
            ? FullName.Substring(0, 1).ToUpper()
            : "T";
    }
}