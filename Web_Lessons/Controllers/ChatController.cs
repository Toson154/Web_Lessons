// Controllers/ChatController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Web_Lessons.Hubs;
using Web_Lessons.Models;
using Web_Lessons.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Web_Lessons.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ILogger<StudentController> _logger;

        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IWebHostEnvironment _webHostEnvironment; // Add this

        public ChatController(

            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<ChatHub> hubContext,
            ILogger<StudentController> logger,
            IWebHostEnvironment webHostEnvironment) // Add this parameter
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _webHostEnvironment = webHostEnvironment; // Assign it
        }

        // GET: /Chat
        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            var currentUser = await _userManager.GetUserAsync(User);

            // Get all chats for current user
            var chats = await _context.Chats
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .Where(c => c.User1Id == currentUserId || c.User2Id == currentUserId)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();

            // Get other user info
            var chatViewModels = new List<ChatViewModel>();
            foreach (var chat in chats)
            {
                var otherUser = chat.User1Id == currentUserId ? chat.User2 : chat.User1;
                var lastMessage = chat.Messages?.FirstOrDefault();
                var unreadCount = await _context.ChatMessages
                    .CountAsync(m => m.ChatId == chat.Id &&
                                    m.SenderId != currentUserId &&
                                    !m.IsRead);

                chatViewModels.Add(new ChatViewModel
                {
                    Id = chat.Id,
                    OtherUserId = otherUser.Id,
                    OtherUserName = otherUser.FullName,
                    OtherUserProfileImage = otherUser.ProfileImageUrl,
                    IsTeacher = otherUser.IsTeacher,
                    LastMessage = lastMessage?.Content,
                    LastMessageAt = lastMessage?.CreatedAt,
                    UnreadCount = unreadCount,
                    IsOnline = ChatHub.IsUserOnline(otherUser.Id)
                });
            }

            // Get teachers for new chat (students only)
            var teachers = new List<ApplicationUser>();
            if (!currentUser.IsTeacher)
            {
                teachers = await _userManager.Users
                    .Where(u => u.IsTeacher)
                    .ToListAsync();
            }

            return View(new ChatIndexViewModel
            {
                Chats = chatViewModels,
                AvailableTeachers = teachers.Select(t => new Web_Lessons.ViewModels.TeacherViewModel
                {
                    Id = t.Id,
                    Name = t.FullName,
                    ProfileImageUrl = t.ProfileImageUrl
                }).ToList()
            });
        }

        // GET: /Chat/{id}
        // في ChatController.cs - إجراء Details المصحح
        // في ChatController.cs - إجراء Details المعدل
        // في ChatController.cs - تصحيح إجراء SearchTeachers
        [HttpGet]
        public async Task<IActionResult> SearchTeachers(string search = "")
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);

                var teachers = await _userManager.Users
                    .Where(u => u.IsTeacher &&
                               u.Id != currentUserId &&
                               (string.IsNullOrEmpty(search) ||
                                u.FullName.Contains(search) ||
                                u.Email.Contains(search)))
                    .OrderBy(u => u.FullName)
                    .Select(t => new Web_Lessons.ViewModels.TeacherViewModel
                    {
                        Id = t.Id,
                        Name = t.FullName,
                        ProfileImageUrl = t.ProfileImageUrl ?? "/images/avatar.png",
                        Email = t.Email,
                        Bio = t.Bio, // هذا أصبح متاح الآن
                        StudentCount = _context.Enrollments
                            .Count(e => e.Course.Subject.TeacherId == t.Id),
                        CourseCount = _context.Courses
                            .Count(c => c.Subject.TeacherId == t.Id),
                        SubjectCount = _context.Subjects
                            .Count(s => s.TeacherId == t.Id),
                        IsOnline = ChatHub.IsUserOnline(t.Id) // هذا أصبح متاح الآن
                    })
                    .Take(20)
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    teachers = teachers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching teachers");
                return Json(new
                {
                    success = false,
                    teachers = new List<TeacherViewModel>()
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> CheckTeacherStatus(string teacherId)
        {
            try
            {
                var teacher = await _userManager.FindByIdAsync(teacherId);
                if (teacher == null)
                {
                    return Json(new { success = false, message = "Teacher not found" });
                }

                var isOnline = ChatHub.IsUserOnline(teacherId);
                var lastSeen = "Recently";

                if (!isOnline && teacher.LastLogin.HasValue)
                {
                    var timeDiff = DateTime.UtcNow - teacher.LastLogin.Value;
                    if (timeDiff.TotalMinutes < 5) lastSeen = "Just now";
                    else if (timeDiff.TotalHours < 1) lastSeen = $"{(int)timeDiff.TotalMinutes} min ago";
                    else if (timeDiff.TotalDays < 1) lastSeen = $"{(int)timeDiff.TotalHours} hours ago";
                    else lastSeen = teacher.LastLogin.Value.ToString("MMM dd");
                }

                return Json(new
                {
                    success = true,
                    isOnline = isOnline,
                    lastSeen = lastSeen,
                    teacherName = teacher.FullName,
                    teacherImage = teacher.ProfileImageUrl ?? "/images/avatar.png"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking teacher status");
                return Json(new { success = false, message = "Error checking status" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);

                // الحصول على المحادثة مع جميع البيانات
                var chat = await _context.Chats
                    .Include(c => c.User1)
                    .Include(c => c.User2)
                    .FirstOrDefaultAsync(c => c.Id == id &&
                        (c.User1Id == currentUserId || c.User2Id == currentUserId));

                if (chat == null)
                {
                    return RedirectToAction("Index", new { error = "Chat not found" });
                }

                // الحصول على المستخدم الآخر
                var otherUser = chat.User1Id == currentUserId ? chat.User2 : chat.User1;

                // الحصول على آخر 50 رسالة فقط
                var messages = await _context.ChatMessages
                    .Where(m => m.ChatId == id)
                    .Include(m => m.Sender)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(50)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new ChatMessageViewModel
                    {
                        Id = m.Id,
                        ChatId = m.ChatId,
                        Content = m.Content,
                        SenderId = m.SenderId,
                        SenderName = m.Sender.FullName,
                        SenderProfileImage = m.Sender.ProfileImageUrl ?? "/images/avatar.png",
                        IsOwnMessage = m.SenderId == currentUserId,
                        IsRead = m.IsRead,
                        CreatedAt = m.CreatedAt,
                        TimeAgo = GetTimeAgo(m.CreatedAt),
                        AttachmentUrl = m.AttachmentUrl,
                        AttachmentType = m.AttachmentType
                    })
                    .ToListAsync();

                // تحديث الرسائل غير المقروءة (في الخلفية)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var unreadMessages = await _context.ChatMessages
                            .Where(m => m.ChatId == id &&
                                       m.SenderId != currentUserId &&
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
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error marking messages as read");
                    }
                });

                // تحقق من حالة الاتصال للمستخدم الآخر
                var isOnline = ChatHub.IsUserOnline(otherUser.Id);

                // إعداد البيانات للـ ViewBag (مطابقة للفيو)
                ViewBag.CurrentChatId = chat.Id;
                ViewBag.CurrentUserId = currentUserId; // إضافة هذا المهم
                ViewBag.OtherUserId = otherUser.Id;
                ViewBag.OtherUserName = otherUser.FullName;
                ViewBag.OtherUserIsTeacher = otherUser.IsTeacher;
                ViewBag.OtherUserProfileImage = otherUser.ProfileImageUrl ?? "/images/avatar.png";
                ViewBag.OtherUserIsOnline = isOnline;
                ViewBag.UserProfileImage = currentUser.ProfileImageUrl ?? "/images/avatar.png";
                ViewBag.UserFullName = currentUser.FullName;
                ViewBag.UserFirstName = currentUser.FullName?.Split(' ')[0] ?? "User";
                ViewBag.UnreadNotificationsCount = await _context.Notifications
                    .CountAsync(n => n.UserId == currentUserId && !n.IsRead);
                ViewBag.UnreadMessagesCount = await _context.ChatMessages
                    .CountAsync(m => (m.Chat.User1Id == currentUserId || m.Chat.User2Id == currentUserId) &&
                                    m.SenderId != currentUserId && !m.IsRead);

                // إعداد الـ ViewModel
                var model = new ChatDetailViewModel
                {
                    Chat = new ChatViewModel
                    {
                        Id = chat.Id,
                        OtherUserId = otherUser.Id,
                        OtherUserName = otherUser.FullName,
                        OtherUserProfileImage = otherUser.ProfileImageUrl ?? "/images/avatar.png",
                        IsTeacher = otherUser.IsTeacher,
                        IsOnline = isOnline,
                        LastMessage = messages.LastOrDefault()?.Content,
                        LastMessageAt = messages.LastOrDefault()?.CreatedAt,
                        UnreadCount = 0 // سيتم تحديثها عبر AJAX
                    },
                    Messages = messages,
                    NewMessage = new SendMessageViewModel
                    {
                        ChatId = chat.Id
                    }
                };

                return View("Details", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chat details");
                return RedirectToAction("Index", new { error = "Error loading chat" });
            }
        }

        // في ChatController.cs - إضافة هذا الإجراء
        [HttpGet]
        public async Task<IActionResult> GetChats()
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);

                var chats = await _context.Chats
                    .Include(c => c.User1)
                    .Include(c => c.User2)
                    .Where(c => c.User1Id == currentUserId || c.User2Id == currentUserId)
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

                var chatViewModels = new List<ChatViewModel>();

                foreach (var chat in chats)
                {
                    var otherUserId = chat.User1Id == currentUserId ? chat.User2Id : chat.User1Id;
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
                                       m.SenderId != currentUserId &&
                                       !m.IsRead);

                    chatViewModels.Add(new ChatViewModel
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
                    currentUserId = currentUserId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chats");
                return Json(new { success = false, chats = new List<ChatViewModel>() });
            }
        }
        // في ChatController.cs - إضافة هذا الإجراء المكتمل
        [HttpGet]
        public async Task<IActionResult> GetCourseTeachers(int courseId)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);

                // الحصول على معلومات الدورة والمعلم
                var course = await _context.Courses
                    .Include(c => c.Subject)
                    .ThenInclude(s => s.Teacher)
                    .FirstOrDefaultAsync(c => c.Id == courseId);

                if (course == null)
                {
                    return Json(new { success = false, message = "Course not found" });
                }

                var teacher = course.Subject?.Teacher;

                if (teacher == null)
                {
                    return Json(new { success = false, message = "Teacher not found" });
                }

                var teacherData = new
                {
                    Id = teacher.Id,
                    Name = teacher.FullName,
                    ProfileImageUrl = teacher.ProfileImageUrl ?? "/images/avatar.png",
                    Email = teacher.Email,
                    Bio = teacher.Bio,
                    CourseCount = await _context.Courses
                        .CountAsync(c => c.Subject.TeacherId == teacher.Id),
                    StudentCount = await _context.Enrollments
                        .CountAsync(e => e.Course.Subject.TeacherId == teacher.Id),
                    IsOnline = ChatHub.IsUserOnline(teacher.Id)
                };

                return Json(new { success = true, teacher = teacherData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course teachers");
                return Json(new { success = false, message = "Error loading teacher information" });
            }
        }

        // في ChatController.cs - إجراء جديد
        [HttpGet]
        public async Task<IActionResult> GetChatMessages(int chatId, int skip = 0, int take = 50)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);

                // التحقق من الوصول
                var canAccess = await _context.Chats
                    .AnyAsync(c => c.Id == chatId &&
                        (c.User1Id == currentUserId || c.User2Id == currentUserId));

                if (!canAccess)
                {
                    return Json(new { success = false, message = "Access denied" });
                }

                // الحصول على الرسائل
                var messages = await _context.ChatMessages
                    .Where(m => m.ChatId == chatId)
                    .Include(m => m.Sender)
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .Select(m => new ChatMessageViewModel
                    {
                        Id = m.Id,
                        ChatId = m.ChatId,
                        Content = m.Content,
                        SenderId = m.SenderId,
                        SenderName = m.Sender.FullName,
                        SenderProfileImage = m.Sender.ProfileImageUrl ?? "/images/avatar.png",
                        IsOwnMessage = m.SenderId == currentUserId,
                        IsRead = m.IsRead,
                        CreatedAt = m.CreatedAt,
                        TimeAgo = GetTimeAgo(m.CreatedAt),
                        AttachmentUrl = m.AttachmentUrl,
                        AttachmentType = m.AttachmentType
                    })
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                // تحديث الرسائل غير المقروءة
                if (messages.Any(m => !m.IsRead && m.SenderId != currentUserId))
                {
                    var unreadMessages = messages
                        .Where(m => !m.IsRead && m.SenderId != currentUserId)
                        .Select(m => m.Id)
                        .ToList();

                    // تحديث في الخلفية
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var messagesToUpdate = await _context.ChatMessages
                                .Where(m => unreadMessages.Contains(m.Id))
                                .ToListAsync();

                            foreach (var msg in messagesToUpdate)
                            {
                                msg.IsRead = true;
                                msg.ReadAt = DateTime.UtcNow;
                            }

                            await _context.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error marking messages as read");
                        }
                    });
                }

                return Json(new { success = true, messages = messages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat messages");
                return Json(new { success = false, message = "Error loading messages" });
            }
        }

      
       
        // POST: /Chat/Create
        // في ChatController.cs - إجراء Create المصحح
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateChatViewModel model)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);

                // التحقق من صحة البيانات
                if (currentUserId == model.TeacherId)
                {
                    TempData["ErrorMessage"] = "Cannot start a chat with yourself";
                    return RedirectToAction("Index");
                }

                var teacher = await _userManager.FindByIdAsync(model.TeacherId);
                if (teacher == null || !teacher.IsTeacher)
                {
                    TempData["ErrorMessage"] = "User is not a teacher";
                    return RedirectToAction("Index");
                }

                // التحقق من وجود محادثة سابقة
                var existingChat = await _context.Chats
                    .FirstOrDefaultAsync(c =>
                        (c.User1Id == currentUserId && c.User2Id == model.TeacherId) ||
                        (c.User1Id == model.TeacherId && c.User2Id == currentUserId));

                if (existingChat != null)
                {
                    // إذا كانت هناك رسالة أولية، أضفها
                    if (!string.IsNullOrEmpty(model.InitialMessage))
                    {
                        var message = new ChatMessage
                        {
                            ChatId = existingChat.Id,
                            SenderId = currentUserId,
                            Content = model.InitialMessage.Trim(),
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
                        redirect = Url.Action("Details", new { id = existingChat.Id })
                    });
                }

                // إنشاء محادثة جديدة
                var chat = new Chat
                {
                    User1Id = currentUserId,
                    User2Id = model.TeacherId,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = null
                };

                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();

                // إضافة رسالة أولية إذا كانت موجودة
                if (!string.IsNullOrEmpty(model.InitialMessage))
                {
                    var message = new ChatMessage
                    {
                        ChatId = chat.Id,
                        SenderId = currentUserId,
                        Content = model.InitialMessage.Trim(),
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
                    redirect = Url.Action("Details", new { id = chat.Id })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chat");
                return Json(new
                {
                    success = false,
                    message = "Error creating chat: " + ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(SendMessageViewModel model)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);

                // التحقق من وجود المحادثة
                var chat = await _context.Chats
                    .FirstOrDefaultAsync(c => c.Id == model.ChatId &&
                        (c.User1Id == currentUserId || c.User2Id == currentUserId));

                if (chat == null)
                {
                    return Json(new { success = false, message = "Chat not found" });
                }

                // إنشاء الرسالة
                var message = new ChatMessage
                {
                    ChatId = model.ChatId,
                    SenderId = currentUserId,
                    Content = model.Content?.Trim() ?? "",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    ReadAt = DateTime.MinValue
                };

                _context.ChatMessages.Add(message);
                chat.LastMessageAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // تحديث ViewModel للرسالة
                var messageViewModel = new ChatMessageViewModel
                {
                    Id = message.Id,
                    ChatId = message.ChatId,
                    Content = message.Content,
                    SenderId = message.SenderId,
                    SenderName = currentUser.FullName,
                    SenderProfileImage = currentUser.ProfileImageUrl ?? "/images/avatar.png",
                    IsOwnMessage = true,
                    IsRead = false,
                    CreatedAt = message.CreatedAt,
                    TimeAgo = GetTimeAgo(message.CreatedAt)
                };

                return Json(new
                {
                    success = true,
                    messageId = message.Id,
                    timestamp = message.CreatedAt.ToString("hh:mm tt"),
                    message = messageViewModel
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        private static string GetTimeAgo(DateTime date)
        {
            var timeSpan = DateTime.UtcNow - date;

            if (timeSpan.TotalDays > 365)
                return $"{(int)(timeSpan.TotalDays / 365)} years ago";
            if (timeSpan.TotalDays > 30)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";
            if (timeSpan.TotalDays > 1)
                return $"{(int)timeSpan.TotalDays} days ago";
            if (timeSpan.TotalHours > 1)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalMinutes > 1)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";

            return "just now";
        }
        [HttpGet]
        public async Task<IActionResult> CreateNew(string teacherId, int? courseId = null)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);

                // التحقق من أن المعلم موجود
                var teacher = await _userManager.FindByIdAsync(teacherId);
                if (teacher == null || !teacher.IsTeacher)
                {
                    TempData["ErrorMessage"] = "Teacher not found";
                    return RedirectToAction("Index");
                }

                // التحقق من وجود محادثة سابقة
                var existingChat = await _context.Chats
                    .FirstOrDefaultAsync(c =>
                        (c.User1Id == currentUserId && c.User2Id == teacherId) ||
                        (c.User1Id == teacherId && c.User2Id == currentUserId));

                // إذا كانت المحادثة موجودة، انتقل إليها
                if (existingChat != null)
                {
                    return RedirectToAction("Details", new { id = existingChat.Id });
                }

                // إنشاء نموذج لمحادثة جديدة
                var model = new CreateChatViewModel
                {
                    TeacherId = teacherId
                };

                // إضافة معلومات إضافية إذا كانت موجودة
                ViewBag.TeacherName = teacher.FullName;
                ViewBag.TeacherProfileImage = teacher.ProfileImageUrl ?? "/images/avatar.png";
                ViewBag.CourseId = courseId;

                // الحصول على معلومات الدورة إذا كانت موجودة
                if (courseId.HasValue)
                {
                    var course = await _context.Courses
                        .Include(c => c.Subject)
                        .FirstOrDefaultAsync(c => c.Id == courseId);

                    if (course != null)
                    {
                        ViewBag.CourseTitle = course.Title;
                        model.InitialMessage = $"Hi, I have a question about your course '{course.Title}'";
                    }
                }

                return View("Create", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateNew");
                TempData["ErrorMessage"] = "Error creating chat";
                return RedirectToAction("Index");
            }
        }

        private string GetAttachmentType(string contentType)
        {
            return contentType.ToLower() switch
            {
                var ct when ct.StartsWith("image/") => "image",
                var ct when ct.StartsWith("video/") => "video",
                var ct when ct.Contains("pdf") => "pdf",
                var ct when ct.Contains("word") => "document",
                _ => "file"
            };
        }
    }
}