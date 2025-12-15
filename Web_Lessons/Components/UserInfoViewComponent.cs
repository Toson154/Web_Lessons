using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Web_Lessons.Models;

namespace Web_Lessons.Components
{
    [ViewComponent(Name = "UserInfo")]
    public class UserInfoViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserInfoViewComponent(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // استخدام ViewContext.User بدلاً من User مباشرة
            if (!ViewContext.HttpContext.User.Identity.IsAuthenticated)
            {
                return Content(string.Empty);
            }

            var user = await _userManager.GetUserAsync(ViewContext.HttpContext.User);
            if (user == null)
            {
                return Content(string.Empty);
            }

            var model = new UserInfoViewModel
            {
                FullName = user.FullName,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/avatar.png",
                IsTeacher = user.IsTeacher,
                Email = user.Email
            };

            return View(model);
        }
    }

    public class UserInfoViewModel
    {
        public string FullName { get; set; }
        public string ProfileImageUrl { get; set; }
        public bool IsTeacher { get; set; }
        public string Email { get; set; }

        public string FirstName => FullName?.Split(' ')[0] ?? "User";
        public string UserInitial => !string.IsNullOrEmpty(FullName)
            ? FullName.Substring(0, 1).ToUpper()
            : "U";
    }
}