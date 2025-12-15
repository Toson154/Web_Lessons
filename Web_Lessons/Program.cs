using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.ResponseCompression;
using Web_Lessons.Models;
using Web_Lessons.Hubs;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Web_Lessons.Middleware;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// 1. Configure Response Compression FIRST
// -------------------------------------------------------
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/javascript",
        "text/css",
        "text/html",
        "application/json",
        "text/json",
        "application/xml",
        "text/xml",
        "text/plain"
    });
});

// Configure compression levels
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// -------------------------------------------------------
// 2. Configure Response Caching
// -------------------------------------------------------
builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 1024 * 1024; // 1MB
    options.SizeLimit = 100 * 1024 * 1024; // 100MB
    options.UseCaseSensitivePaths = false;
});

// -------------------------------------------------------
// 3. Configure Database
// -------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration
    .GetConnectionString("DefaultConnection")));

// -------------------------------------------------------
// 4. Configure Identity
// -------------------------------------------------------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;

    options.User.RequireUniqueEmail = true;

    // Security settings
    options.SignIn.RequireConfirmedAccount = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// -------------------------------------------------------
// 5. Add SignalR Service
// -------------------------------------------------------
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400; // 100KB
});

// -------------------------------------------------------
// 6. Cookie Settings
// -------------------------------------------------------
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);

    // Remember Me
    options.Events.OnValidatePrincipal = context =>
    {
        if (context.Properties.IsPersistent)
        {
            context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
        }
        return Task.CompletedTask;
    };
});

// -------------------------------------------------------
// 7. إعدادات رفع الملفات الكبيرة
// -------------------------------------------------------
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 2147483648; // 2GB
    options.MultipartHeadersLengthLimit = int.MaxValue;
    options.MultipartBoundaryLengthLimit = int.MaxValue;
    options.BufferBodyLengthLimit = 2147483648;
});

// إعداد Kestrel للسماح بطلبات كبيرة
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 2147483648; // 2GB
});

// إعدادات الرفع
builder.Services.AddAntiforgery(options =>
{
    options.SuppressXFrameOptionsHeader = true;
});

// -------------------------------------------------------
// 8. Authorization Policies
// -------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("TeacherOnly", policy =>
        policy.RequireRole("Teacher"));

    options.AddPolicy("StudentOnly", policy =>
        policy.RequireRole("Student"));

    options.AddPolicy("ActiveTeacher", policy =>
    {
        policy.RequireRole("Teacher");
        policy.RequireClaim("AccountStatus", "Active");
    });
});

// -------------------------------------------------------
// 9. Add Controllers + Views + Razor Pages
// -------------------------------------------------------
var mvcBuilder = builder.Services.AddControllersWithViews(options =>
{
    // زيادة حجم الطلبات المسموح به
    options.MaxModelBindingRecursionDepth = 32;

    // إعدادات لرفع الملفات الكبيرة
    options.MaxModelBindingCollectionSize = 1024 * 1024 * 1024; // 1GB
    options.MaxValidationDepth = 32;
});

// تفعيل RuntimeCompilation فقط في بيئة التطوير
if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}

builder.Services.AddRazorPages();

// -------------------------------------------------------
// 10. Add Session
// -------------------------------------------------------
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// -------------------------------------------------------
// 11. Add IWebHostEnvironment for file uploads
// -------------------------------------------------------
builder.Services.AddSingleton<IWebHostEnvironment>(builder.Environment);

// -------------------------------------------------------
// 12. Add Logging
// -------------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// -------------------------------------------------------
// 13. Add HTTP Context Accessor
// -------------------------------------------------------
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// -------------------------------------------------------
// 14. Middleware Pipeline - IMPORTANT ORDER!
// -------------------------------------------------------

// 1. Exception Handling FIRST
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // Development settings
    app.UseDeveloperExceptionPage();
    app.UseHttpsRedirection();
}

// 2. Response Compression - يجب أن يأتي باكراً
app.UseResponseCompression();

// 3. Response Caching
app.UseResponseCaching();

// 4. Custom Middleware
app.UseMiddleware<PerformanceMiddleware>();

// 5. زيادة حجم الطلبات المسموح به
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

// 6. Static Files Configuration
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 1 week
        ctx.Context.Response.Headers.Append(
            "Cache-Control", "public, max-age=604800");
    }
});

// 7. For file uploads
var uploadsPath = Path.Combine(builder.Environment.WebRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
    Directory.CreateDirectory(Path.Combine(uploadsPath, "videos"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "pdfs"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "temp"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "profile-images"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "subject-images"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "course-images"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "lesson-pdfs"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "chat"));

    Console.WriteLine("✅ Created upload directories");
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    ServeUnknownFileTypes = true,
    OnPrepareResponse = ctx =>
    {
        // السماح بعرض جميع أنواع الملفات
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");

        // التحكم في الـ Caching للملفات المرفوعة
        var cacheMaxAgeOneWeek = (60 * 60 * 24 * 7).ToString();
        ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={cacheMaxAgeOneWeek}");
    }
});

// 8. Routing
app.UseRouting();

// 9. Custom Error Middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

// 10. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 11. Session
app.UseSession();

// -------------------------------------------------------
// 15. SEEDING ADMIN & DEFAULT DATA
// -------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // ✅ 1. Ensure Database is created
    await dbContext.Database.EnsureCreatedAsync();

    // ✅ 2. Create Roles if they don't exist
    string[] roles = { "Admin", "Teacher", "Student" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            Console.WriteLine($"✅ Created role: {role}");
        }
    }

    // ✅ 3. Create Admin User if doesn't exist
    var adminEmail = "admin@weblessons.com";
    var adminPassword = "Admin@123";

    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Administrator",
            IsTeacher = true,
            CreatedOn = DateTime.UtcNow,
            ProfileImageUrl = "/images/default-avatar.png"
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            await userManager.AddToRoleAsync(adminUser, "Teacher");

            // Add claims for admin
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, adminUser.FullName),
                new Claim(ClaimTypes.Email, adminUser.Email),
                new Claim("AccountStatus", "Active"),
                new Claim("IsSuperAdmin", "true"),
                new Claim("RegisteredOn", DateTime.UtcNow.ToString("u"))
            };

            await userManager.AddClaimsAsync(adminUser, claims);
            Console.WriteLine("✅ Admin user created successfully!");
        }
        else
        {
            Console.WriteLine("❌ Failed to create admin user:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"   - {error.Description}");
            }
        }
    }

    // ✅ 4. Create Sample Teacher (Optional)
    var teacherEmail = "teacher@weblessons.com";
    ApplicationUser teacherUser = null;
    if (await userManager.FindByEmailAsync(teacherEmail) == null)
    {
        teacherUser = new ApplicationUser
        {
            UserName = teacherEmail,
            Email = teacherEmail,
            FullName = "Demo Teacher",
            IsTeacher = true,
            CreatedOn = DateTime.UtcNow,
            ProfileImageUrl = "/images/default-avatar.png",
            Bio = "Mathematics teacher with 10 years experience"
        };

        var teacherPassword = "Teacher@123";
        var teacherResult = await userManager.CreateAsync(teacherUser, teacherPassword);
        if (teacherResult.Succeeded)
        {
            await userManager.AddToRoleAsync(teacherUser, "Teacher");
            Console.WriteLine("✅ Demo teacher created successfully!");
        }
    }
    else
    {
        teacherUser = await userManager.FindByEmailAsync(teacherEmail);
    }

    // ✅ 5. Create Sample Student (Optional)
    var studentEmail = "student@weblessons.com";
    ApplicationUser studentUser = null;
    if (await userManager.FindByEmailAsync(studentEmail) == null)
    {
        studentUser = new ApplicationUser
        {
            UserName = studentEmail,
            Email = studentEmail,
            FullName = "Demo Student",
            IsTeacher = false,
            CreatedOn = DateTime.UtcNow,
            ProfileImageUrl = "/images/default-avatar.png",
            Bio = "Passionate about learning new things"
        };

        var studentPassword = "Student@123";
        var studentResult = await userManager.CreateAsync(studentUser, studentPassword);
        if (studentResult.Succeeded)
        {
            await userManager.AddToRoleAsync(studentUser, "Student");
            Console.WriteLine("✅ Demo student created successfully!");
        }
    }
    else
    {
        studentUser = await userManager.FindByEmailAsync(studentEmail);
    }

    // ✅ 6. Seed Sample Data with Chat, Notifications, Comments, and Reactions
    await SeedSampleDataAsync(dbContext, userManager, teacherUser, studentUser);
}

// -------------------------------------------------------
// 16. Map SignalR Hub
// -------------------------------------------------------
app.MapHub<ChatHub>("/chatHub");

// -------------------------------------------------------
// 17. Routes
// -------------------------------------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// -------------------------------------------------------
// 18. Error Handling
// -------------------------------------------------------
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Unhandled exception: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");

        if (context.Response.HasStarted)
        {
            throw;
        }

        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("An internal server error occurred.");
    }

    if (context.Response.StatusCode == 404)
    {
        context.Request.Path = "/Home/Error";
        await next();
    }
});

app.Run();

// -------------------------------------------------------
// Enhanced Sample Data Seeding Method with Chat, Notifications, Comments, and Reactions
// -------------------------------------------------------
static async Task SeedSampleDataAsync(AppDbContext context, UserManager<ApplicationUser> userManager,
    ApplicationUser teacherUser, ApplicationUser studentUser)
{
    // Check if we need to create sample data
    if (!context.Subjects.Any())
    {
        if (teacherUser != null && studentUser != null)
        {
            // Create sample subject with image
            var subject = new Subject
            {
                Name = "Mathematics",
                Description = "Learn mathematics from basics to advanced topics",
                TeacherId = teacherUser.Id,
                ImageUrl = "/images/default-subject.jpg",
                CreatedAt = DateTime.UtcNow
            };

            await context.Subjects.AddAsync(subject);
            await context.SaveChangesAsync();

            // Create sample course with image
            var course = new Course
            {
                Title = "Algebra Basics",
                Description = "Introduction to algebraic expressions and equations",
                Level = "Beginner",
                SubjectId = subject.Id,
                IsPublished = true,
                ThumbnailUrl = "/images/default-course.jpg",
                CreatedAt = DateTime.UtcNow
            };

            await context.Courses.AddAsync(course);
            await context.SaveChangesAsync();

            // Create sample lessons
            var lessons = new List<Lesson>
            {
                new Lesson
                {
                    Title = "Introduction to Variables",
                    Description = "Learn about variables and how to use them in algebra",
                    VideoUrl = "/uploads/videos/sample-video.mp4",
                    CourseId = course.Id,
                    Order = 1,
                    DurationMinutes = 30,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Lesson
                {
                    Title = "Basic Equations",
                    Description = "Solving simple algebraic equations",
                    VideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                    PdfUrl = "/uploads/pdfs/sample-equations.pdf",
                    CourseId = course.Id,
                    Order = 2,
                    DurationMinutes = 45,
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new Lesson
                {
                    Title = "Linear Equations",
                    Description = "Understanding and solving linear equations",
                    CourseId = course.Id,
                    Order = 3,
                    DurationMinutes = 50,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };

            await context.Lessons.AddRangeAsync(lessons);
            await context.SaveChangesAsync();

            // Create enrollment for student
            var enrollment = new Enrollment
            {
                StudentId = studentUser.Id,
                CourseId = course.Id,
                EnrolledAt = DateTime.UtcNow.AddDays(-2)
            };

            await context.Enrollments.AddAsync(enrollment);
            await context.SaveChangesAsync();

            // Create lesson progress
            var progress = new LessonProgress
            {
                StudentId = studentUser.Id,
                LessonId = lessons[0].Id,
                IsCompleted = true,
                StartedAt = DateTime.UtcNow.AddDays(-1),
                CompletedAt = DateTime.UtcNow,
                TimeSpentMinutes = 25
            };

            await context.LessonProgresses.AddAsync(progress);
            await context.SaveChangesAsync();

            // Create a main comment
            var mainComment = new Comment
            {
                Content = "Great lesson! Very clear explanation about variables.",
                UserId = studentUser.Id,
                LessonId = lessons[0].Id,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            };

            await context.Comments.AddAsync(mainComment);
            await context.SaveChangesAsync();

            // Create reply from teacher
            var replyComment = new Comment
            {
                Content = "Thank you! I'm glad you found it helpful. Let me know if you have any questions.",
                UserId = teacherUser.Id,
                LessonId = lessons[0].Id,
                ParentCommentId = mainComment.Id,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            };

            await context.Comments.AddAsync(replyComment);
            await context.SaveChangesAsync();

            // Create comment reactions
            var reactions = new List<CommentReaction>
                {
                    new CommentReaction
                    {
                        UserId = studentUser.Id,
                        CommentId = mainComment.Id,
                        ReactionType = "like",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                    },
                    new CommentReaction
                    {
                        UserId = teacherUser.Id,
                        CommentId = mainComment.Id,
                        ReactionType = "love",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-25)
                    }
                };

            await context.CommentReactions.AddRangeAsync(reactions);
            await context.SaveChangesAsync();

            // Create a chat between student and teacher
            var chat = new Chat
            {
                User1Id = studentUser.Id, // Student
                User2Id = teacherUser.Id, // Teacher
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                LastMessageAt = DateTime.UtcNow.AddHours(-1)
            };

            await context.Chats.AddAsync(chat);
            await context.SaveChangesAsync();

            // Create sample chat messages
            var chatMessages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        ChatId = chat.Id,
                        SenderId = studentUser.Id,
                        Content = "Hello Teacher, I have a question about the variables lesson.",
                        CreatedAt = DateTime.UtcNow.AddHours(-3),
                        IsRead = true
                    },
                    new ChatMessage
                    {
                        ChatId = chat.Id,
                        SenderId = teacherUser.Id,
                        Content = "Hi there! Sure, what's your question?",
                        CreatedAt = DateTime.UtcNow.AddHours(-2.5),
                        IsRead = true,
                        ReadAt = DateTime.UtcNow.AddHours(-2)
                    },
                    new ChatMessage
                    {
                        ChatId = chat.Id,
                        SenderId = studentUser.Id,
                        Content = "I'm confused about independent vs dependent variables. Can you explain?",
                        CreatedAt = DateTime.UtcNow.AddHours(-2),
                        IsRead = true
                    },
                    new ChatMessage
                    {
                        ChatId = chat.Id,
                        SenderId = teacherUser.Id,
                        Content = "Of course! Independent variables are the ones we change, dependent variables change as a result.",
                        CreatedAt = DateTime.UtcNow.AddHours(-1),
                        IsRead = false
                    }
                };

            await context.ChatMessages.AddRangeAsync(chatMessages);
            await context.SaveChangesAsync();

            // Create sample notifications
            var notifications = new List<Notification>
                {
                    new Notification
                    {
                        UserId = studentUser.Id,
                        Type = "new_reply",
                        Title = "رد جديد على تعليقك",
                        Message = "المعلم رد على تعليقك في درس Introduction to Variables",
                        RelatedId = mainComment.Id,
                        RelatedType = "comment",
                        CreatedAt = DateTime.UtcNow.AddHours(-1),
                        IsRead = true,
                        ReadAt = DateTime.UtcNow.AddMinutes(-30)
                    },
                    new Notification
                    {
                        UserId = studentUser.Id,
                        Type = "reaction",
                        Title = "تفاعل جديد",
                        Message = "المعلم تفاعل بحب مع تعليقك",
                        RelatedId = mainComment.Id,
                        RelatedType = "comment",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-25),
                        IsRead = false
                    },
                    new Notification
                    {
                        UserId = teacherUser.Id,
                        Type = "new_comment",
                        Title = "تعليق جديد",
                        Message = "الطالب أضاف تعليقاً جديداً على درس Introduction to Variables",
                        RelatedId = mainComment.Id,
                        RelatedType = "comment",
                        CreatedAt = DateTime.UtcNow.AddHours(-2),
                        IsRead = true,
                        ReadAt = DateTime.UtcNow.AddHours(-1.5)
                    }
                };

            await context.Notifications.AddRangeAsync(notifications);
            await context.SaveChangesAsync();

            // Create another course for variety
            var course2 = new Course
            {
                Title = "Geometry Fundamentals",
                Description = "Learn about shapes, angles, and spatial relationships",
                Level = "Intermediate",
                SubjectId = subject.Id,
                IsPublished = true,
                ThumbnailUrl = "/images/default-course.jpg",
                CreatedAt = DateTime.UtcNow
            };

            await context.Courses.AddAsync(course2);
            await context.SaveChangesAsync();

            // Create another subject
            var subject2 = new Subject
            {
                Name = "Computer Science",
                Description = "Programming and computer fundamentals",
                TeacherId = teacherUser.Id,
                ImageUrl = "/images/default-subject.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            await context.Subjects.AddAsync(subject2);
            await context.SaveChangesAsync();

            // Create computer science course
            var csCourse = new Course
            {
                Title = "Web Development Basics",
                Description = "Learn HTML, CSS, and JavaScript fundamentals",
                Level = "Beginner",
                SubjectId = subject2.Id,
                IsPublished = true,
                ThumbnailUrl = "/images/default-course.jpg",
                CreatedAt = DateTime.UtcNow
            };

            await context.Courses.AddAsync(csCourse);
            await context.SaveChangesAsync();

            Console.WriteLine("✅ Enhanced sample data created successfully!");
            Console.WriteLine("   - 2 Subjects (Mathematics, Computer Science)");
            Console.WriteLine("   - 3 Courses");
            Console.WriteLine("   - 3 Lessons");
            Console.WriteLine("   - 1 Enrollment");
            Console.WriteLine("   - 1 Completed Lesson Progress");
            Console.WriteLine("   - 2 Comments (with reply)");
            Console.WriteLine("   - 2 Comment Reactions");
            Console.WriteLine("   - 1 Chat with 4 messages");
            Console.WriteLine("   - 3 Notifications");
        }
        else
        {
            Console.WriteLine("⚠️  Teacher or student user not found. Skipping sample data creation.");
        }
    }
    else
    {
        Console.WriteLine("✅ Sample data already exists.");
    }
}