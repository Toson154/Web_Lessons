// في ViewModels/TeacherViewModel.cs أو أينما يوجد
namespace Web_Lessons.ViewModels
{
    public class TeacherViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Email { get; set; }
        public int StudentCount { get; set; }
        public int CourseCount { get; set; }
        public int SubjectCount { get; set; } // أضف هذا
        public string Bio { get; set; } // أضف هذا
        public bool IsOnline { get; set; } // أضف هذا
    }
}