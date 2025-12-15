using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web_Lessons.Models;
using Web_Lessons.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO; // أضف هذا
using System.Linq;
using System.Threading.Tasks;
using Web_Lessons.Models;
using Web_Lessons.Hubs;
using Microsoft.AspNetCore.SignalR;
using Web_Lessons.Hubs;

namespace Web_Lessons.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<StudentController> _logger;
        private readonly IHubContext<ChatHub> _hubContext; // أضف هذا

        public StudentController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<StudentController> logger,
            IHubContext<ChatHub> hubContext)

        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _hubContext = hubContext; // Initialize it

        }


        // إجراء SaveNotes لحفظ الملاحظات


        // في StudentController.cs - أضف هذه الدالة الجديدة
        [HttpGet]
        public async Task<IActionResult> GetChatsOptimized()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);

                var chats = await (from c in _context.Chats
                                   where c.User1Id == userId || c.User2Id == userId
                                   let otherUser = (c.User1Id == userId) ? c.User2 : c.User1
                                   let lastMessage = _context.ChatMessages
                                       .Where(m => m.ChatId == c.Id)
                                       .OrderByDescending(m => m.CreatedAt)
                                       .FirstOrDefault()
                                   let unreadCount = _context.ChatMessages
                                       .Count(m => m.ChatId == c.Id &&
                                                  m.SenderId != userId &&
                                                  !m.IsRead)
                                   select new ChatListViewModel
                                   {
                                       Id = c.Id,
                                       OtherUserId = otherUser.Id,
                                       OtherUserName = otherUser.FullName,
                                       OtherUserProfileImage = otherUser.ProfileImageUrl ?? "/images/avatar.png",
                                       IsTeacher = otherUser.IsTeacher,
                                       LastMessage = lastMessage != null ?
                                           (lastMessage.Content.Length > 25 ?
                                            lastMessage.Content.Substring(0, 25) + "..." :
                                            lastMessage.Content) : null,
                                       LastMessageAt = lastMessage != null ? lastMessage.CreatedAt : null,
                                       UnreadCount = unreadCount,
                                       IsOnline = ChatHub.IsUserOnline(otherUser.Id),
                                       ChatCreatedAt = c.CreatedAt // الآن فيه CreatedAt
                                   })
                                  .OrderByDescending(c => c.LastMessageAt ?? c.ChatCreatedAt)
                                  .Take(20)
                                  .ToListAsync();

                return Json(new
                {
                    success = true,
                    chats = chats,
                    currentUserId = userId,
                    canStartNewChat = !currentUser.IsTeacher
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading optimized chats");
                return Json(new
                {
                    success = false,
                    message = "Error loading chats",
                    chats = new List<ChatListViewModel>()
                });
            }
        }

        // إضافة دالة لبدء محادثة جديدة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartNewChat(string teacherId, string initialMessage = null)
        {
            try
            {
                var studentId = _userManager.GetUserId(User);
                var student = await _userManager.GetUserAsync(User);

                // التحقق من أن المستخدم طالب
                if (student.IsTeacher)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Only students can start new chats"
                    });
                }

                // التحقق من أن المستهدف معلم
                var teacher = await _userManager.FindByIdAsync(teacherId);
                if (teacher == null || !teacher.IsTeacher)
                {
                    return Json(new
                    {
                        success = false,
                        message = "User is not a teacher"
                    });
                }

                // التحقق من وجود محادثة سابقة
                var existingChat = await _context.Chats
                    .FirstOrDefaultAsync(c =>
                        (c.User1Id == studentId && c.User2Id == teacherId) ||
                        (c.User1Id == teacherId && c.User2Id == studentId));

                if (existingChat != null)
                {
                    return Json(new
                    {
                        success = true,
                        chatId = existingChat.Id,
                        message = "Chat already exists",
                        isNew = false
                    });
                }

                // إنشاء محادثة جديدة
                var chat = new Chat
                {
                    User1Id = studentId,
                    User2Id = teacherId,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = null
                };

                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();

                // إضافة رسالة أولية إذا وجدت
                if (!string.IsNullOrEmpty(initialMessage))
                {
                    var message = new ChatMessage
                    {
                        ChatId = chat.Id,
                        SenderId = studentId,
                        Content = initialMessage,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        ReadAt = DateTime.MinValue
                    };

                    _context.ChatMessages.Add(message);
                    chat.LastMessageAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                // إرسال إشعار للمعلم
                var notification = new Notification
                {
                    UserId = teacherId,
                    Type = "new_message",
                    Title = "New Chat Started",
                    Message = $"{student.FullName} started a new chat with you",
                    RelatedId = chat.Id,
                    RelatedType = "chat",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    chatId = chat.Id,
                    message = "Chat created successfully",
                    isNew = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting new chat");
                return Json(new
                {
                    success = false,
                    message = "Error creating chat: " + ex.Message
                });
            }
        }
        private async Task SendNotificationToTeacher(int lessonId, ApplicationUser student, string message)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .ThenInclude(c => c.Subject)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson?.Course?.Subject?.TeacherId != null)
            {
                var notification = new Notification
                {
                    UserId = lesson.Course.Subject.TeacherId,
                    Type = "notes_saved",
                    Title = "New Notes",
                    Message = message,
                    RelatedId = lessonId,
                    RelatedType = "lesson",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send via SignalR
                await _hubContext.Clients.User(lesson.Course.Subject.TeacherId)
                    .SendAsync("ReceiveNotification", new
                    {
                        notification.Id,
                        notification.Type,
                        notification.Title,
                        notification.Message,
                        notification.RelatedId,
                        notification.RelatedType,
                        notification.CreatedAt
                    });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNotification(string type, int relatedId, string extraData = null)
        {
            try
            {
                var studentId = await GetCurrentUserId();
                var student = await GetCurrentUser();

                // تحديد المستلمين بناءً على نوع الإشعار
                List<string> recipientIds = new List<string>();
                string title = "";
                string message = "";
                string relatedType = "";

                switch (type)
                {
                    case "new_comment":
                        // إشعار للمعلم عند إضافة تعليق جديد
                        var lesson = await _context.Lessons
                            .Include(l => l.Course)
                            .ThenInclude(c => c.Subject)
                            .FirstOrDefaultAsync(l => l.Id == relatedId);

                        if (lesson != null)
                        {
                            recipientIds.Add(lesson.Course.Subject.TeacherId);
                            title = "تعليق جديد";
                            message = $"{student.FullName} أضاف تعليقاً جديداً على درس {lesson.Title}";
                            relatedType = "lesson";
                        }
                        break;

                    case "lesson_completed":
                        // إشعار للمعلم عند إكمال درس
                        var completedLesson = await _context.Lessons
                            .Include(l => l.Course)
                            .ThenInclude(c => c.Subject)
                            .FirstOrDefaultAsync(l => l.Id == relatedId);

                        if (completedLesson != null)
                        {
                            recipientIds.Add(completedLesson.Course.Subject.TeacherId);
                            title = "درس مكتمل";
                            message = $"{student.FullName} أكمل درس {completedLesson.Title}";
                            relatedType = "lesson";
                        }
                        break;

                    case "reaction":
                        // إشعار لصاحب التعليق عند التفاعل
                        var comment = await _context.Comments
                            .Include(c => c.User)
                            .FirstOrDefaultAsync(c => c.Id == relatedId);

                        if (comment != null && comment.UserId != studentId)
                        {
                            recipientIds.Add(comment.UserId);
                            title = "تفاعل جديد";
                            var reactionType = extraData ?? "like";
                            message = $"{student.FullName} تفاعل {GetReactionMessage(reactionType)} مع تعليقك";
                            relatedType = "comment";
                        }
                        break;

                    case "new_reply":
                        // إشعار لصاحب التعليق عند الرد
                        var parentComment = await _context.Comments
                            .Include(c => c.User)
                            .FirstOrDefaultAsync(c => c.Id == relatedId);

                        if (parentComment != null && parentComment.UserId != studentId)
                        {
                            recipientIds.Add(parentComment.UserId);
                            title = "رد جديد";
                            message = $"{student.FullName} رد على تعليقك";
                            relatedType = "comment";
                        }
                        break;

                    case "mention":
                        // إشعار عند ذكر مستخدم
                        if (!string.IsNullOrEmpty(extraData))
                        {
                            recipientIds.Add(extraData);
                            var mentionedInComment = await _context.Comments
                                .FirstOrDefaultAsync(c => c.Id == relatedId);

                            title = "تم ذكرك";
                            message = $"{student.FullName} ذكرك في تعليق";
                            relatedType = "comment";
                        }
                        break;

                    case "notes_saved":
                        // إشعار للمعلم عند حفظ ملاحظات
                        var lessonForNotes = await _context.Lessons
                            .Include(l => l.Course)
                            .ThenInclude(c => c.Subject)
                            .FirstOrDefaultAsync(l => l.Id == relatedId);

                        if (lessonForNotes != null)
                        {
                            recipientIds.Add(lessonForNotes.Course.Subject.TeacherId);
                            title = "ملاحظات جديدة";
                            message = $"{student.FullName} حفظ ملاحظات على درس {lessonForNotes.Title}";
                            relatedType = "lesson";
                        }
                        break;

                    default:
                        return Json(new { success = false, message = "نوع الإشعار غير معروف" });
                }

                // إرسال الإشعارات للمستلمين
                foreach (var recipientId in recipientIds.Distinct())
                {
                    var notification = new Notification
                    {
                        UserId = recipientId,
                        Type = type,
                        Title = title,
                        Message = message,
                        RelatedId = relatedId,
                        RelatedType = relatedType,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.Notifications.Add(notification);

                    // إرسال إشعار فوري عبر SignalR
                    await _hubContext.Clients.User(recipientId)
                        .SendAsync("ReceiveNotification", new
                        {
                            notification.Id,
                            notification.Type,
                            notification.Title,
                            notification.Message,
                            notification.RelatedId,
                            notification.RelatedType,
                            notification.CreatedAt
                        });
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "تم إرسال الإشعار بنجاح",
                    recipients = recipientIds.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending notification: {type} for ID: {relatedId}");
                return Json(new { success = false, message = "حدث خطأ أثناء إرسال الإشعار" });
            }
        }

        // Helper method لرسائل التفاعل

        // إجراء SendNotification لإرسال إشعارات
        // Helper method لمعرفة نوع الإشعار
        private string GetRelatedType(string notificationType)
        {
            return notificationType switch
            {
                "new_comment" => "lesson",
                "lesson_completed" => "lesson",
                "reaction" => "comment",
                "new_reply" => "comment",
                "mention" => "comment",
                _ => "general"
            };
        }

        // Helper method لرسائل التفاعل
        private string GetReactionMessage(string reactionType)
        {
            return reactionType switch
            {
                "like" => "بإعجاب",
                "love" => "بحب",
                "haha" => "بضحك",
                "wow" => "بدهشة",
                "sad" => "بحزن",
                "angry" => "بغضب",
                _ => "بإعجاب"
            };
        }
        // في StudentController.cs - إصلاح إجراء GetLessonComments
        [HttpGet]
        public async Task<IActionResult> GetLessonComments(int lessonId, int page = 1, int pageSize = 10)
        {
            try
            {
                var studentId = await GetCurrentUserId();

                // الحصول على إجمالي التعليقات
                var totalComments = await _context.Comments
                    .CountAsync(c => c.LessonId == lessonId &&
                                   !c.IsDeleted &&
                                   c.ParentCommentId == null);

                // الحصول على التعليقات مع User وليس المعلم
                var comments = await _context.Comments
                    .Include(c => c.User)  // هذا مهم - يجب أن يكون User موجود
                    .Include(c => c.Reactions)
                    .Where(c => c.LessonId == lessonId &&
                               !c.IsDeleted &&
                               c.ParentCommentId == null)
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new CommentViewModel
                    {
                        Id = c.Id,
                        Content = c.Content,
                        UserId = c.UserId,
                        UserName = c.User.FullName ?? "Unknown User",  // ضمان وجود قيمة
                        UserProfileImage = c.User.ProfileImageUrl ?? "/images/avatar.png",
                        IsTeacher = c.User != null && c.User.IsTeacher,  // التحقق من null
                        IsEdited = c.IsEdited,
                        CreatedAt = c.CreatedAt,
                        TimeAgo = GetTimeAgo(c.CreatedAt),
                        Reactions = c.Reactions
                            .GroupBy(r => r.ReactionType)
                            .ToDictionary(g => g.Key, g => g.Count()),
                        CurrentUserReaction = c.Reactions
                            .Where(r => r.UserId == studentId)
                            .Select(r => r.ReactionType)
                            .FirstOrDefault(),
                        RepliesCount = _context.Comments
                            .Count(rc => rc.ParentCommentId == c.Id && !rc.IsDeleted)
                    })
                    .ToListAsync();

                // الحصول على الردود للتعليقات التي تحتوي على ردود
                foreach (var comment in comments)
                {
                    if (comment.RepliesCount > 0)
                    {
                        comment.Replies = await _context.Comments
                            .Include(c => c.User)  // هذا مهم للردود أيضاً
                            .Include(c => c.Reactions)
                            .Where(c => c.ParentCommentId == comment.Id && !c.IsDeleted)
                            .OrderBy(c => c.CreatedAt)
                            .Take(5)
                            .Select(c => new CommentViewModel
                            {
                                Id = c.Id,
                                Content = c.Content,
                                UserId = c.UserId,
                                UserName = c.User != null ? c.User.FullName : "Unknown User",
                                UserProfileImage = c.User != null ? c.User.ProfileImageUrl ?? "/images/avatar.png" : "/images/avatar.png",
                                IsTeacher = c.User != null && c.User.IsTeacher,
                                IsEdited = c.IsEdited,
                                CreatedAt = c.CreatedAt,
                                TimeAgo = GetTimeAgo(c.CreatedAt),
                                Reactions = c.Reactions
                                    .GroupBy(r => r.ReactionType)
                                    .ToDictionary(g => g.Key, g => g.Count()),
                                CurrentUserReaction = c.Reactions
                                    .Where(r => r.UserId == studentId)
                                    .Select(r => r.ReactionType)
                                    .FirstOrDefault()
                            })
                            .ToListAsync();
                    }
                }

                return Json(new
                {
                    success = true,
                    comments = comments,
                    totalComments = totalComments,
                    totalPages = (int)Math.Ceiling((double)totalComments / pageSize),
                    currentPage = page,
                    hasMore = page * pageSize < totalComments
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lesson comments");
                return Json(new
                {
                    success = false,
                    message = "Error loading comments",
                    comments = new List<CommentViewModel>()
                });
            }
        }

        // في StudentController.cs - إضافة إجراء StartChatFromLesson
        // في StudentController.cs - إضافة هذا الإجراء لمراسلة المعلم
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartChatFromLesson(string teacherId, int lessonId, string initialMessage = null)
        {
            try
            {
                var studentId = await GetCurrentUserId();
                var student = await GetCurrentUser();

                _logger.LogInformation($"Starting chat from lesson {lessonId} with teacher {teacherId}");

                // التحقق من أن المستخدم طالب
                if (student.IsTeacher)
                {
                    return Json(new { success = false, message = "Only students can start chats" });
                }

                // التحقق من أن المعلم موجود
                var teacher = await _userManager.FindByIdAsync(teacherId);
                if (teacher == null || !teacher.IsTeacher)
                {
                    _logger.LogWarning($"Teacher not found: {teacherId}");
                    return Json(new { success = false, message = "Teacher not found" });
                }

                // الحصول على معلومات الدرس
                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.Id == lessonId);

                if (lesson == null)
                {
                    return Json(new { success = false, message = "Lesson not found" });
                }

                // التحقق من وجود محادثة سابقة
                var existingChat = await _context.Chats
                    .FirstOrDefaultAsync(c =>
                        (c.User1Id == studentId && c.User2Id == teacherId) ||
                        (c.User1Id == teacherId && c.User2Id == studentId));

                if (existingChat != null)
                {
                    // إضافة رسالة إذا كانت موجودة
                    if (!string.IsNullOrEmpty(initialMessage))
                    {
                        var message = new ChatMessage
                        {
                            ChatId = existingChat.Id,
                            SenderId = studentId,
                            Content = initialMessage.Trim(),
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false,
                            ReadAt = DateTime.MinValue
                        };

                        _context.ChatMessages.Add(message);
                        existingChat.LastMessageAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }

                    return Json(new
                    {
                        success = true,
                        chatId = existingChat.Id,
                        redirectUrl = Url.Action("Details", "Chat", new { id = existingChat.Id }),
                        message = "Chat already exists"
                    });
                }

                // إنشاء محادثة جديدة
                var chat = new Chat
                {
                    User1Id = studentId,
                    User2Id = teacherId,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = null
                };

                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();

                // إضافة رسالة أولية إذا كانت موجودة
                if (!string.IsNullOrEmpty(initialMessage))
                {
                    var message = new ChatMessage
                    {
                        ChatId = chat.Id,
                        SenderId = studentId,
                        Content = initialMessage.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        ReadAt = DateTime.MinValue
                    };

                    _context.ChatMessages.Add(message);
                    chat.LastMessageAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"Chat created: {chat.Id} between {studentId} and {teacherId}");

                return Json(new
                {
                    success = true,
                    chatId = chat.Id,
                    redirectUrl = Url.Action("Details", "Chat", new { id = chat.Id }),
                    message = "Chat created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error starting chat from lesson {lessonId}");
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // إجراء إضافي: تحميل الملاحظات المحفوظة
        [HttpGet]
        public async Task<IActionResult> GetSavedNotes(int lessonId)
        {
            try
            {
                var studentId = await GetCurrentUserId();

                var note = await _context.LessonNotes
                    .FirstOrDefaultAsync(n => n.LessonId == lessonId && n.StudentId == studentId);

                if (note == null)
                {
                    return Json(new { success = true, notes = "", hasNotes = false });
                }

                return Json(new
                {
                    success = true,
                    notes = note.Content,
                    hasNotes = !string.IsNullOrEmpty(note.Content),
                    lastUpdated = note.UpdatedAt.ToString("yyyy/MM/dd HH:mm")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting notes for lesson {lessonId}");
                return Json(new { success = false, message = "Error loading notes" });
            }
        }

        // إجراء إضافي: حذف الملاحظات
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotes(int lessonId)
        {
            try
            {
                var studentId = await GetCurrentUserId();

                var note = await _context.LessonNotes
                    .FirstOrDefaultAsync(n => n.LessonId == lessonId && n.StudentId == studentId);

                if (note != null)
                {
                    _context.LessonNotes.Remove(note);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "تم حذف الملاحظات بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting notes for lesson {lessonId}");
                return Json(new { success = false, message = "حدث خطأ أثناء حذف الملاحظات" });
            }
        }

        // إجراء إضافي: تحديث إشعار بالمحادثات الجديدة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NotifyNewMessage(int chatId, string message)
        {
            try
            {
                var studentId = await GetCurrentUserId();
                var student = await GetCurrentUser();

                var chat = await _context.Chats
                    .Include(c => c.User1)
                    .Include(c => c.User2)
                    .FirstOrDefaultAsync(c => c.Id == chatId);

                if (chat == null)
                {
                    return Json(new { success = false, message = "المحادثة غير موجودة" });
                }

                // تحديد المستلم (المعلم)
                var recipientId = chat.User1Id == studentId ? chat.User2Id : chat.User1Id;

                // إرسال إشعار للمعلم
                var notification = new Notification
                {
                    UserId = recipientId,
                    Type = "new_message",
                    Title = "رسالة جديدة",
                    Message = $"{student.FullName}: {message}",
                    RelatedId = chatId,
                    RelatedType = "chat",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);

                // إرسال عبر SignalR
                await _hubContext.Clients.User(recipientId)
                    .SendAsync("ReceiveNotification", new
                    {
                        notification.Id,
                        notification.Type,
                        notification.Title,
                        notification.Message,
                        notification.RelatedId,
                        notification.RelatedType,
                        notification.CreatedAt
                    });

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تم إرسال الإشعار بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message notification for chat {chatId}");
                return Json(new { success = false, message = "حدث خطأ أثناء إرسال الإشعار" });
            }
        }


        #region Helper Methods
        private async Task<string> GetCurrentUserId()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.Id;
        }

        private async Task<ApplicationUser> GetCurrentUser()
        {
            return await _userManager.GetUserAsync(User);
        }

        private async Task<bool> IsEnrolledInCourse(int courseId, string studentId = null)
        {
            studentId ??= await GetCurrentUserId();
            return await _context.Enrollments
                .AnyAsync(e => e.CourseId == courseId && e.StudentId == studentId);
        }

        private async Task<bool> IsCourseAccessible(int courseId)
        {
            return await _context.Courses
                .AnyAsync(c => c.Id == courseId && c.IsPublished);
        }
        #endregion
        public async Task<IActionResult> MyProgress()
        {
            try
            {
                var studentId = await GetCurrentUserId();

                // Get all courses the student is enrolled in
                var enrollments = await _context.Enrollments
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Subject)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Lessons)
                    .Where(e => e.StudentId == studentId)
                    .ToListAsync();

                var courseProgress = new List<CourseProgressViewModel>();

                foreach (var enrollment in enrollments)
                {
                    var course = enrollment.Course;
                    if (course == null) continue;

                    var totalLessons = course.Lessons?.Count ?? 0;
                    var completedLessons = await _context.LessonProgresses
                        .CountAsync(lp => lp.StudentId == studentId &&
                                         lp.Lesson.CourseId == course.Id &&
                                         lp.IsCompleted);

                    var progressPercentage = totalLessons > 0
                        ? (int)Math.Round((double)completedLessons / totalLessons * 100)
                        : 0;

                    courseProgress.Add(new CourseProgressViewModel
                    {
                        CourseId = course.Id,
                        CourseTitle = course.Title,
                        SubjectName = course.Subject?.Name ?? "Unknown",
                        CompletedLessons = completedLessons,
                        TotalLessons = totalLessons,
                        ProgressPercentage = progressPercentage,
                        EnrollmentDate = enrollment.EnrolledAt.ToString("yyyy/MM/dd")
                    });
                }

                return View(courseProgress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MyProgress action");

                // Return empty list instead of crashing
                return View(new List<CourseProgressViewModel>());
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int courseId)
        {
            try
            {
                var studentId = await GetCurrentUserId();

                // Check if course exists and is published
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Id == courseId && c.IsPublished);

                if (course == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Course not found or not available"
                    });
                }

                // Check if already enrolled
                var existingEnrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);

                if (existingEnrollment != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "You are already enrolled in this course"
                    });
                }

                // Create enrollment
                var enrollment = new Enrollment
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    EnrolledAt = DateTime.UtcNow
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Successfully enrolled in the course!",
                    redirect = Url.Action("CourseDetails", "Student", new { id = courseId })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Enroll action");
                return Json(new
                {
                    success = false,
                    message = "Error enrolling in course: " + ex.Message
                });
            }
        }
        #region Dashboard - محسنة مع بيانات حقيقية

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                _logger.LogInformation($"Loading dashboard for student: {userId}");

                // Get student's enrolled courses with teacher info
                var enrolledCourses = await _context.Enrollments
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Subject)
                        .ThenInclude(s => s.Teacher)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Lessons)
                    .Where(e => e.StudentId == userId)
                    .Select(e => e.Course)
                    .Take(4)
                    .ToListAsync();

                // Get available courses (not enrolled)
                var availableCourses = await _context.Courses
                    .Include(c => c.Subject)
                        .ThenInclude(s => s.Teacher)
                    .Where(c => c.IsPublished &&
                           !_context.Enrollments.Any(e => e.StudentId == userId && e.CourseId == c.Id))
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(4)
                    .ToListAsync();

                // Calculate progress
                var totalLessons = enrolledCourses.Sum(c => c.Lessons?.Count ?? 0);
                var completedLessons = await _context.LessonProgresses
                    .CountAsync(lp => lp.StudentId == userId && lp.IsCompleted);

                var progressPercentage = totalLessons > 0
                    ? (int)Math.Round((double)completedLessons / totalLessons * 100)
                    : 0;

                // Get recent activities
                var recentActivities = await GetRecentActivities(userId);

                // Calculate available lessons
                var availableLessonsCount = await _context.Lessons
                    .CountAsync(l => l.Course.Subject != null &&
                                   _context.Enrollments.Any(e =>
                                       e.StudentId == userId &&
                                       e.CourseId == l.CourseId));

                // Get notifications count
                var unreadNotificationsCount = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                // Get today's learning time
                var todayLearningTime = await GetTodayLearningTime(userId);

                // Get learning streak
                var learningStreak = await GetLearningStreak(userId);

                // Prepare ViewBag data for layout
                ViewBag.UserFullName = user?.FullName ?? "Student";
                ViewBag.UserFirstName = user?.FullName?.Split(' ')[0] ?? "Student";
                ViewBag.UserProfileImage = user?.ProfileImageUrl ?? "/images/avatar.png";
                ViewBag.UnreadNotificationsCount = unreadNotificationsCount;

                var model = new StudentDashboardViewModel
                {
                    StudentName = user?.FullName ?? "Student",
                    StudentEmail = user?.Email ?? "student@example.com",
                    ProfileImageUrl = user?.ProfileImageUrl ?? "/images/avatar.png",
                    EnrolledCoursesCount = enrolledCourses.Count,
                    AvailableCoursesCount = availableCourses.Count,
                    TotalLessons = totalLessons,
                    CompletedLessons = completedLessons,
                    ProgressPercentage = progressPercentage,
                    JoinDate = user?.CreatedOn ?? DateTime.UtcNow,
                    EnrolledCourses = enrolledCourses,
                    AvailableCourses = availableCourses,
                    RecentActivities = recentActivities,
                    AvailableLessonsCount = availableLessonsCount,
                    UnreadNotificationsCount = unreadNotificationsCount,
                    DailyLearningGoal = user?.DailyLearningGoal ?? 60,
                    TodayLearningTime = todayLearningTime,
                    LearningStreak = learningStreak
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading student dashboard");

                // Return basic data on error
                var user = await _userManager.GetUserAsync(User);
                ViewBag.UserFullName = user?.FullName ?? "Student";
                ViewBag.UserFirstName = user?.FullName?.Split(' ')[0] ?? "Student";
                ViewBag.UserProfileImage = user?.ProfileImageUrl ?? "/images/avatar.png";
                ViewBag.UnreadNotificationsCount = 0;

                return View(new StudentDashboardViewModel
                {
                    StudentName = user?.FullName ?? "Student"
                });
            }
        }
        private async Task<int> GetUnreadNotificationsCount(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        // Helper method for today's learning time
        private async Task<int> GetTodayLearningTime(string studentId)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            return await _context.LessonProgresses
                .Where(lp => lp.StudentId == studentId &&
                            lp.IsCompleted &&
                            lp.CompletedAt >= today &&
                            lp.CompletedAt < tomorrow)
                .SumAsync(lp => lp.TimeSpentMinutes ?? 0);
        }

        // Helper method for learning streak

        // Helper method for learning streak
        private async Task<int> GetLearningStreak(string userId)
        {
            // Simple implementation - count days with at least one completed lesson in last 7 days
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7).Date;
            var streakDays = await _context.LessonProgresses
                .Where(lp => lp.StudentId == userId &&
                           lp.IsCompleted &&
                           lp.CompletedAt >= sevenDaysAgo)
                .Select(lp => lp.CompletedAt.Value.Date)
                .Distinct()
                .CountAsync();

            return streakDays;
        }
        private async Task<List<ActivityViewModel>> GetRecentActivities(string studentId)
        {
            var activities = new List<ActivityViewModel>();

            // الأنشطة من آخر 7 أيام
            var lastWeek = DateTime.UtcNow.AddDays(-7);

            // 1. الدروس المكتملة
            var completedLessons = await _context.LessonProgresses
                .Include(lp => lp.Lesson)
                .ThenInclude(l => l.Course)
                .Where(lp => lp.StudentId == studentId &&
                           lp.CompletedAt >= lastWeek)
                .OrderByDescending(lp => lp.CompletedAt)
                .Take(3)
                .Select(lp => new ActivityViewModel
                {
                    Type = "درس مكتمل",
                    Title = lp.Lesson.Title,
                    Description = $"أكملت درس في دورة {lp.Lesson.Course.Title}",
                    Icon = "fas fa-check-circle",
                    Color = "success",
                    CreatedAt = lp.CompletedAt.Value,
                    Link = Url.Action("LessonDetails", new { id = lp.LessonId })
                })
                .ToListAsync();

            activities.AddRange(completedLessons);

            // 2. التعليقات المضافة
            var comments = await _context.Comments
                .Include(c => c.Lesson)
                .ThenInclude(l => l.Course)
                .Where(c => c.UserId == studentId &&
                          c.CreatedAt >= lastWeek &&
                          !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Take(3)
                .Select(c => new ActivityViewModel
                {
                    Type = "تعليق جديد",
                    Title = c.Lesson.Title,
                    Description = "أضفت تعليقاً جديداً",
                    Icon = "fas fa-comment",
                    Color = "info",
                    CreatedAt = c.CreatedAt,
                    Link = Url.Action("LessonDetails", new { id = c.LessonId }) + "#comments"
                })
                .ToListAsync();

            activities.AddRange(comments);

            // 3. التسجيل في دورات جديدة
            var newEnrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == studentId &&
                          e.EnrolledAt >= lastWeek)
                .OrderByDescending(e => e.EnrolledAt)
                .Take(3)
                .Select(e => new ActivityViewModel
                {
                    Type = "تسجيل جديد",
                    Title = e.Course.Title,
                    Description = "سجلت في دورة جديدة",
                    Icon = "fas fa-book",
                    Color = "primary",
                    CreatedAt = e.EnrolledAt,
                    Link = Url.Action("CourseDetails", new { id = e.CourseId })
                })
                .ToListAsync();

            activities.AddRange(newEnrollments);

            // ترتيب حسب التاريخ وتحديد أول 5
            return activities
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToList();
        }

        private async Task<List<UpcomingLessonViewModel>> GetUpcomingLessons(string studentId)
        {
            var upcomingLessons = new List<UpcomingLessonViewModel>();

            // الحصول على جميع الدروس من الدورات المسجلة
            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            var lessons = await _context.Lessons
                .Include(l => l.Course)
                .Where(l => enrolledCourseIds.Contains(l.CourseId))
                .OrderBy(l => l.Order)
                .Take(10)
                .ToListAsync();

            foreach (var lesson in lessons)
            {
                var isCompleted = await _context.LessonProgresses
                    .AnyAsync(lp => lp.StudentId == studentId &&
                                  lp.LessonId == lesson.Id &&
                                  lp.IsCompleted);

                if (!isCompleted)
                {
                    upcomingLessons.Add(new UpcomingLessonViewModel
                    {
                        LessonId = lesson.Id,
                        Title = lesson.Title,
                        CourseTitle = lesson.Course.Title,
                        Duration = lesson.DurationMinutes,
                        Order = lesson.Order,
                        IsCompleted = false,
                        CourseId = lesson.CourseId
                    });

                    if (upcomingLessons.Count >= 5) break;
                }
            }

            return upcomingLessons;
        }

        private async Task<WeeklyStatsViewModel> GetWeeklyStats(string studentId)
        {
            var stats = new WeeklyStatsViewModel();
            var today = DateTime.UtcNow.Date;

            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var nextDay = date.AddDays(1);

                // الدروس المكتملة في هذا اليوم
                var completedCount = await _context.LessonProgresses
                    .CountAsync(lp => lp.StudentId == studentId &&
                                    lp.IsCompleted &&
                                    lp.CompletedAt >= date &&
                                    lp.CompletedAt < nextDay);

                // الوقت المستغرق في هذا اليوم (بالدقائق)
                var timeSpent = await _context.LessonProgresses
                    .Where(lp => lp.StudentId == studentId &&
                               lp.CompletedAt >= date &&
                               lp.CompletedAt < nextDay)
                    .SumAsync(lp => lp.TimeSpentMinutes ?? 0);

                stats.Days.Add(date.ToString("ddd"));
                stats.CompletedLessons.Add(completedCount);
                stats.TimeSpent.Add(timeSpent / 60.0); // تحويل لساعات
            }

            stats.TotalCompleted = stats.CompletedLessons.Sum();
            stats.AverageTimePerDay = stats.TimeSpent.Any() ?
                stats.TimeSpent.Average() : 0;

            return stats;
        }

        private async Task<List<SubjectStatsViewModel>> GetTopSubjects(string studentId)
        {
            var subjectStats = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Subject)
                .Where(e => e.StudentId == studentId)
                .GroupBy(e => e.Course.Subject)
                .Select(g => new SubjectStatsViewModel
                {
                    SubjectId = g.Key.Id,
                    SubjectName = g.Key.Name,
                    CourseCount = g.Count(),
                    TotalLessons = g.SelectMany(e => e.Course.Lessons).Count(),
                    CompletedLessons = _context.LessonProgresses
                        .Count(lp => lp.StudentId == studentId &&
                                    lp.Lesson.Course.SubjectId == g.Key.Id &&
                                    lp.IsCompleted),
                    ProgressPercentage = 0 // سيتم حسابها لاحقاً
                })
                .OrderByDescending(s => s.CourseCount)
                .Take(3)
                .ToListAsync();

            foreach (var stat in subjectStats)
            {
                stat.ProgressPercentage = stat.TotalLessons > 0 ?
                    (int)Math.Round((double)stat.CompletedLessons / stat.TotalLessons * 100) : 0;
            }

            return subjectStats;
        }
        #endregion

        public async Task<IActionResult> BrowseCourses(
    string search = null,
    string level = null,
    int? subjectId = null,
    string sortBy = "newest")
        {
            try
            {
                // Get current student ID
                var studentId = await GetCurrentUserId();

                // Start with all published courses
                var query = _context.Courses
                    .Include(c => c.Subject)
                    .Include(c => c.Lessons)
                    .Include(c => c.Enrollments)
                    .Where(c => c.IsPublished)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c =>
                        c.Title.Contains(search) ||
                        c.Description.Contains(search) ||
                        (c.Subject != null && c.Subject.Name.Contains(search)));
                }

                if (!string.IsNullOrEmpty(level))
                {
                    query = query.Where(c => c.Level == level);
                }

                if (subjectId.HasValue)
                {
                    query = query.Where(c => c.SubjectId == subjectId);
                }

                // Apply sorting
                switch (sortBy.ToLower())
                {
                    case "popular":
                        query = query.OrderByDescending(c => c.Enrollments.Count);
                        break;
                    case "newest":
                        query = query.OrderByDescending(c => c.CreatedAt);
                        break;
                    default:
                        query = query.OrderByDescending(c => c.CreatedAt);
                        break;
                }

                // Execute query
                var courses = await query.ToListAsync();

                // Get enrolled course IDs
                var enrolledCourseIds = await _context.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .Select(e => e.CourseId)
                    .ToListAsync();

                // Prepare view data
                ViewBag.Subjects = await _context.Subjects
                    .Where(s => s.Courses.Any(c => c.IsPublished))
                    .Select(s => new { s.Id, s.Name })
                    .ToListAsync();

                ViewBag.Levels = await _context.Courses
                    .Where(c => c.IsPublished)
                    .Select(c => c.Level)
                    .Distinct()
                    .ToListAsync();

                ViewBag.Search = search;
                ViewBag.Level = level;
                ViewBag.SubjectId = subjectId;
                ViewBag.SortBy = sortBy;
                ViewBag.EnrolledCourseIds = enrolledCourseIds;

                return View(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BrowseCourses action");
                TempData["ErrorMessage"] = "Error loading courses. Please try again.";
                return View(new List<Course>());
            }
        }
        // في StudentController.cs - إضافة هذا الإجراء
        [HttpGet]
        public async Task<IActionResult> GetTeachersStatus()
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);

                // الحصول على جميع المعلمين الذين لديهم محادثات مع المستخدم الحالي
                var teacherIds = await _context.Chats
                    .Where(c => c.User1Id == currentUserId || c.User2Id == currentUserId)
                    .Select(c => c.User1Id == currentUserId ? c.User2Id : c.User1Id)
                    .Distinct()
                    .ToListAsync();

                var statusData = new Dictionary<string, object>();

                foreach (var teacherId in teacherIds)
                {
                    var isOnline = ChatHub.IsUserOnline(teacherId);
                    var teacher = await _userManager.FindByIdAsync(teacherId);

                    string lastSeen = "Recently";
                    if (!isOnline && teacher?.LastLogin != null)
                    {
                        var timeDiff = DateTime.UtcNow - teacher.LastLogin.Value;
                        if (timeDiff.TotalMinutes < 5) lastSeen = "Just now";
                        else if (timeDiff.TotalHours < 1) lastSeen = $"{(int)timeDiff.TotalMinutes} min ago";
                        else if (timeDiff.TotalDays < 1) lastSeen = $"{(int)timeDiff.TotalHours} hours ago";
                        else lastSeen = teacher.LastLogin.Value.ToString("MMM dd");
                    }

                    statusData[teacherId] = new
                    {
                        isOnline = isOnline,
                        lastSeen = lastSeen,
                        teacherName = teacher?.FullName ?? "Unknown",
                        teacherImage = teacher?.ProfileImageUrl ?? "/images/avatar.png"
                    };
                }

                return Json(new { success = true, status = statusData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teachers status");
                return Json(new { success = false, status = new Dictionary<string, object>() });
            }
        }
        #region My Courses
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> MyCourses()
        {
            try
            {
                var studentId = await GetCurrentUserId();

                // استعلام محسن
                var enrollmentData = await _context.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Subject)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Lessons)
                    .OrderByDescending(e => e.EnrolledAt)
                    .Take(12)
                    .Select(e => new
                    {
                        Enrollment = e,
                        Course = e.Course,
                        Subject = e.Course.Subject,
                        TeacherId = e.Course.Subject.TeacherId,
                        LessonCount = e.Course.Lessons.Count
                    })
                    .ToListAsync();

                var courseViewModels = new List<StudentCourseViewModel>();

                foreach (var data in enrollmentData)
                {
                    var enrollment = data.Enrollment;
                    var course = data.Course;

                    // حساب التقدم
                    var completedLessons = await _context.LessonProgresses
                        .CountAsync(lp => lp.StudentId == studentId &&
                                        lp.Lesson.CourseId == course.Id &&
                                        lp.IsCompleted);

                    var totalLessons = data.LessonCount;
                    var progressPercentage = totalLessons > 0 ?
                        (int)Math.Round((double)completedLessons / totalLessons * 100) : 0;

                    // حساب الوقت المستغرق
                    var timeSpent = await _context.LessonProgresses
                        .Where(lp => lp.StudentId == studentId &&
                                   lp.Lesson.CourseId == course.Id &&
                                   lp.IsCompleted)
                        .SumAsync(lp => lp.TimeSpentMinutes ?? 0);

                    // تحميل المدرس إذا كان موجوداً
                    ApplicationUser teacher = null;
                    if (!string.IsNullOrEmpty(data.TeacherId))
                    {
                        teacher = await _context.Users
                            .Where(u => u.Id == data.TeacherId)
                            .Select(u => new ApplicationUser
                            {
                                Id = u.Id,
                                FullName = u.FullName,
                                Email = u.Email,
                                ProfileImageUrl = u.ProfileImageUrl ?? "/images/avatar.png",
                                Bio = u.Bio,
                                IsTeacher = u.IsTeacher
                            })
                            .FirstOrDefaultAsync();
                    }

                    // الحصول على آخر نشاط
                    var lastActivity = await GetLastActivityInCourse(studentId, course.Id);

                    courseViewModels.Add(new StudentCourseViewModel
                    {
                        Course = course,
                        Enrollment = enrollment,
                        CompletedLessons = completedLessons,
                        TotalLessons = totalLessons,
                        ProgressPercentage = progressPercentage,
                        TimeSpentHours = Math.Round(timeSpent / 60.0, 1),
                        EnrollmentDateFormatted = enrollment.EnrolledAt.ToString("yyyy/MM/dd"),
                        LastActivity = lastActivity,
                        Teacher = teacher // ✅ تعيين المدرس
                    });
                }

                ViewBag.TotalCourses = courseViewModels.Count;
                ViewBag.TotalProgress = courseViewModels.Any() ?
                    (int)courseViewModels.Average(c => c.ProgressPercentage) : 0;

                return View("MyCourses", courseViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MyCourses action");
                return View(new List<StudentCourseViewModel>());
            }
        }
        private async Task<DateTime?> GetLastActivityInCourse(string studentId, int courseId)
        {
            // آخر درس مكتمل
            var lastCompleted = await _context.LessonProgresses
                .Include(lp => lp.Lesson)
                .Where(lp => lp.StudentId == studentId &&
                           lp.Lesson.CourseId == courseId &&
                           lp.IsCompleted)
                .OrderByDescending(lp => lp.CompletedAt)
                .Select(lp => lp.CompletedAt)
                .FirstOrDefaultAsync();

            if (lastCompleted.HasValue)
                return lastCompleted;

            // آخر تعليق
            var lastComment = await _context.Comments
                .Include(c => c.Lesson)
                .Where(c => c.UserId == studentId &&
                          c.Lesson.CourseId == courseId &&
                          !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => (DateTime?)c.CreatedAt)
                .FirstOrDefaultAsync();

            return lastComment;
        }
        #endregion

        #region Course Details
        // في StudentController.cs - إجراء CourseDetails المصحح
        public async Task<IActionResult> CourseDetails(int id)
        {
            try
            {
                var studentId = await GetCurrentUserId();

                // التحقق من وجود الدورة ومنشورة
                var course = await _context.Courses
                    .Include(c => c.Subject)
                        .ThenInclude(s => s.Teacher)
                    .Include(c => c.Lessons)
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsPublished);

                if (course == null)
                {
                    TempData["ErrorMessage"] = "الدورة غير موجودة أو غير منشورة!";
                    return RedirectToAction("BrowseCourses");
                }

                // التحقق من التسجيل
                var isEnrolled = await _context.Enrollments
                    .AnyAsync(e => e.StudentId == studentId && e.CourseId == id);

                if (!isEnrolled)
                {
                    TempData["ErrorMessage"] = "يجب التسجيل في هذه الدورة أولاً!";
                    return RedirectToAction("BrowseCourses");
                }

                // الحصول على إحصائيات الطالب في هذه الدورة
                var completedLessons = await _context.LessonProgresses
                    .CountAsync(lp => lp.StudentId == studentId &&
                                    lp.IsCompleted &&
                                    lp.Lesson.CourseId == id);

                var totalLessons = course.Lessons?.Count ?? 0;
                var progressPercentage = totalLessons > 0 ?
                    (int)Math.Round((double)completedLessons / totalLessons * 100) : 0;

                // الوقت المستغرق في الدورة
                var timeSpent = await _context.LessonProgresses
                    .Where(lp => lp.StudentId == studentId &&
                               lp.Lesson.CourseId == id &&
                               lp.IsCompleted)
                    .SumAsync(lp => lp.TimeSpentMinutes ?? 0);

                // الحصول على حالة إكمال كل درس
                var lessons = course.Lessons?.OrderBy(l => l.Order).ToList() ?? new List<Lesson>();
                var lessonCompletionStatus = new Dictionary<int, bool>();

                foreach (var lesson in lessons)
                {
                    var isCompleted = await _context.LessonProgresses
                        .AnyAsync(lp => lp.StudentId == studentId &&
                                      lp.LessonId == lesson.Id &&
                                      lp.IsCompleted);
                    lessonCompletionStatus[lesson.Id] = isCompleted;
                }

                // آخر 5 تعليقات في الدورة
                var recentComments = await _context.Comments
                    .Include(c => c.User)
                    .Include(c => c.Lesson)
                    .Where(c => c.Lesson.CourseId == id && !c.IsDeleted)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .Select(c => new CourseCommentViewModel
                    {
                        Id = c.Id,
                        Content = c.Content,
                        UserName = c.User.FullName,
                        UserProfileImage = c.User.ProfileImageUrl ?? "/images/default-avatar.png",
                        LessonTitle = c.Lesson.Title,
                        CreatedAt = c.CreatedAt,
                        TimeAgo = GetTimeAgo(c.CreatedAt)
                    })
                    .ToListAsync();

                // عدد الطلاب المسجلين
                var totalStudents = await _context.Enrollments
                    .CountAsync(e => e.CourseId == id);

                // إعداد بيانات المدرس للعرض
                var teacher = course.Subject?.Teacher;
                ViewBag.TeacherName = teacher?.FullName ?? "Unknown Teacher";
                ViewBag.TeacherProfileImage = teacher?.ProfileImageUrl ?? "/images/default-avatar.png";
                ViewBag.TeacherEmail = teacher?.Email;
                ViewBag.TeacherBio = teacher?.Bio;
                ViewBag.TeacherCoursesCount = await _context.Courses
                    .CountAsync(c => c.Subject.TeacherId == teacher.Id);

                // إعداد بيانات الطالب للعرض في ViewBag
                var student = await GetCurrentUser();
                ViewBag.UserProfileImage = student?.ProfileImageUrl ?? "/images/default-avatar.png";
                ViewBag.UserFullName = student?.FullName ?? "Student";
                ViewBag.UserFirstName = student?.FullName?.Split(' ')[0] ?? "Student";

                // إنشاء الـ ViewModel
                var model = new StudentCourseDetailsViewModel
                {
                    Course = course,
                    CompletedLessons = completedLessons,
                    TotalLessons = totalLessons,
                    ProgressPercentage = progressPercentage,
                    TimeSpentHours = Math.Round(timeSpent / 60.0, 1),
                    Lessons = lessons,
                    LessonCompletionStatus = lessonCompletionStatus,
                    RecentComments = recentComments,
                    TotalStudents = totalStudents,
                    CanAccessChat = course.Subject?.TeacherId != null
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CourseDetails action");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل بيانات الدورة";
                return RedirectToAction("MyCourses");
            }
        }
        #endregion

        #region Lesson Details
        public async Task<IActionResult> LessonDetails(int id)
        {
            try
            {
                var studentId = await GetCurrentUserId();
                var student = await GetCurrentUser();

                // الحصول على الدرس
                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                        .ThenInclude(c => c.Subject)
                    .Include(l => l.Course)
                        .ThenInclude(c => c.Lessons)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (lesson == null)
                {
                    TempData["ErrorMessage"] = "Lesson not found";
                    return RedirectToAction("MyCourses");
                }

                // التحقق من التسجيل
                var isEnrolled = await _context.Enrollments
                    .AnyAsync(e => e.StudentId == studentId && e.CourseId == lesson.CourseId);

                if (!isEnrolled)
                {
                    TempData["ErrorMessage"] = "You need to enroll in this course first";
                    return RedirectToAction("CourseDetails", new { id = lesson.CourseId });
                }

                // التحقق من إكمال الدرس
                var isCompleted = await _context.LessonProgresses
                    .AnyAsync(lp => lp.LessonId == id && lp.StudentId == studentId && lp.IsCompleted);

                // دروس الدورة
                var courseLessons = await _context.Lessons
                    .Where(l => l.CourseId == lesson.CourseId)
                    .OrderBy(l => l.Order)
                    .ToListAsync();

                var currentIndex = courseLessons.FindIndex(l => l.Id == id);
                var previousLesson = currentIndex > 0 ? courseLessons[currentIndex - 1] : null;
                var nextLesson = currentIndex < courseLessons.Count - 1 ? courseLessons[currentIndex + 1] : null;

                // إحصائيات
                var completedLessonsInCourse = await _context.LessonProgresses
                    .CountAsync(lp => lp.StudentId == studentId &&
                                    lp.Lesson.CourseId == lesson.CourseId &&
                                    lp.IsCompleted);

                var totalLessonsInCourse = courseLessons.Count;
                var courseProgress = totalLessonsInCourse > 0 ?
                    (int)Math.Round((double)completedLessonsInCourse / totalLessonsInCourse * 100) : 0;

                // إعداد بيانات المعلم في ViewBag
                var courseWithSubject = await _context.Courses
                    .Include(c => c.Subject)
                    .ThenInclude(s => s.Teacher)
                    .FirstOrDefaultAsync(c => c.Id == lesson.CourseId);

                if (courseWithSubject?.Subject?.Teacher != null)
                {
                    var teacher = courseWithSubject.Subject.Teacher;
                    ViewBag.TeacherId = teacher.Id;
                    ViewBag.TeacherName = teacher.FullName;
                    ViewBag.TeacherProfileImage = teacher.ProfileImageUrl ?? "/images/avatar.png";
                    ViewBag.IsTeacherOnline = ChatHub.IsUserOnline(teacher.Id);
                }

                // إعداد بيانات المستخدم في ViewBag
                ViewBag.UserProfileImage = student?.ProfileImageUrl ?? "/images/avatar.png";
                ViewBag.UserFullName = student?.FullName ?? "Student";
                ViewBag.UserFirstName = student?.FullName?.Split(' ')[0] ?? "Student";
                ViewBag.UnreadMessagesCount = await _context.ChatMessages
                    .CountAsync(m => (m.Chat.User1Id == studentId || m.Chat.User2Id == studentId) &&
                                   m.SenderId != studentId && !m.IsRead);

                // إنشاء Model
                var model = new StudentLessonViewModel
                {
                    Lesson = lesson,
                    Course = lesson.Course,
                    IsCompleted = isCompleted,
                    PreviousLesson = previousLesson,
                    NextLesson = nextLesson,
                    TotalLessons = totalLessonsInCourse,
                    CurrentLessonIndex = currentIndex + 1,
                    CompletedLessons = completedLessonsInCourse,
                    TotalLessonsInCourse = totalLessonsInCourse,
                    CourseProgress = courseProgress
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lesson details");
                TempData["ErrorMessage"] = "Error loading lesson";
                return RedirectToAction("MyCourses");
            }
        }
        // في StudentController.cs - إجراء SaveNotes الكامل

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNotes(int lessonId, string notes)
        {
            try
            {
                var studentId = await GetCurrentUserId();
                var student = await GetCurrentUser();

                // التحقق من صحة الدرس
                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.Id == lessonId);

                if (lesson == null)
                {
                    return Json(new { success = false, message = "Lesson not found" });
                }

                // التحقق من تسجيل الطالب في الدورة
                var isEnrolled = await _context.Enrollments
                    .AnyAsync(e => e.StudentId == studentId && e.CourseId == lesson.CourseId);

                if (!isEnrolled)
                {
                    return Json(new { success = false, message = "You are not enrolled in this course" });
                }

                // البحث عن ملاحظات سابقة أو إنشاء جديدة
                var existingNote = await _context.LessonNotes
                    .FirstOrDefaultAsync(n => n.LessonId == lessonId && n.StudentId == studentId);

                if (existingNote != null)
                {
                    // تحديث الملاحظات الحالية
                    existingNote.Content = notes?.Trim();
                    existingNote.UpdatedAt = DateTime.UtcNow;
                    _context.LessonNotes.Update(existingNote);
                }
                else
                {
                    // إنشاء ملاحظات جديدة
                    var lessonNote = new LessonNote
                    {
                        LessonId = lessonId,
                        StudentId = studentId,
                        Content = notes?.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.LessonNotes.Add(lessonNote);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Student {studentId} saved notes for lesson {lessonId}");

                // إرسال إشعار للمعلم إذا كانت هناك ملاحظات
                if (!string.IsNullOrEmpty(notes?.Trim()))
                {
                    await SendNotificationToTeacher(lessonId, student, $"{student.FullName} saved notes for lesson: {lesson.Title}");
                }

                return Json(new
                {
                    success = true,
                    message = "Notes saved successfully",
                    timestamp = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving notes for lesson {lessonId}");
                return Json(new { success = false, message = "Error saving notes: " + ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetTeacherDetails(int courseId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.Subject)
                        .ThenInclude(s => s.Teacher)
                    .FirstOrDefaultAsync(c => c.Id == courseId);

                if (course?.Subject?.Teacher == null)
                {
                    return Json(new { success = false, message = "Teacher not found" });
                }

                var teacher = course.Subject.Teacher;

                // حساب عدد الدورات للمدرس
                var teacherCoursesCount = await _context.Courses
                    .CountAsync(c => c.Subject.TeacherId == teacher.Id);

                // حساب عدد الطلاب للمدرس
                var teacherStudentsCount = await _context.Enrollments
                    .Where(e => e.Course.Subject.TeacherId == teacher.Id)
                    .Select(e => e.StudentId)
                    .Distinct()
                    .CountAsync();

                var teacherData = new
                {
                    id = teacher.Id,
                    fullName = teacher.FullName,
                    email = teacher.Email,
                    profileImage = teacher.ProfileImageUrl ?? "/images/default-avatar.png",
                    bio = teacher.Bio,
                    coursesCount = teacherCoursesCount,
                    studentsCount = teacherStudentsCount,
                    isOnline = ChatHub.IsUserOnline(teacher.Id), // تحتاج لتضمين ChatHub
                    joinDate = teacher.CreatedOn.ToString("yyyy/MM/dd"),
                    lastLogin = teacher.LastLogin?.ToString("yyyy/MM/dd HH:mm")
                };

                return Json(new { success = true, teacher = teacherData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teacher details");
                return Json(new { success = false, message = "Error loading teacher details" });
            }
        }

        // إجراء جديد: إعداد بيانات المستخدم للعرض في جميع الصفحات
        [NonAction]
        private async Task SetupUserData()
        {
            var student = await GetCurrentUser();
            if (student != null)
            {
                ViewBag.UserProfileImage = student.ProfileImageUrl ?? "/images/default-avatar.png";
                ViewBag.UserFullName = student.FullName ?? "Student";
                ViewBag.UserFirstName = student.FullName?.Split(' ')[0] ?? "Student";
                ViewBag.UserEmail = student.Email;

                var studentId = student.Id;
                ViewBag.UnreadNotificationsCount = await _context.Notifications
                    .CountAsync(n => n.UserId == studentId && !n.IsRead);
                ViewBag.UnreadMessagesCount = await _context.ChatMessages
                    .CountAsync(m => (m.Chat.User1Id == studentId || m.Chat.User2Id == studentId)
                        && m.SenderId != studentId && !m.IsRead);
            }
        }

        // دالة SendNotificationToTeacher المعدلة (يجب أن تأخذ 3 باراميترات فقط)

        // إجراء MarkComplete المصحح أيضًا (يوجد فيه نفس الخطأ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkComplete(int lessonId)
        {
            try
            {
                var studentId = await GetCurrentUserId();
                var student = await GetCurrentUser();

                // التحقق من صلاحية الدرس
                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.Id == lessonId);

                if (lesson == null || !await IsEnrolledInCourse(lesson.CourseId, studentId))
                {
                    return Json(new { success = false, message = "غير مصرح" });
                }

                var progress = await _context.LessonProgresses
                    .FirstOrDefaultAsync(lp => lp.LessonId == lessonId && lp.StudentId == studentId);

                var now = DateTime.UtcNow;
                var timeSpent = 0;

                if (progress != null)
                {
                    // حساب الوقت المستغرق
                    if (progress.StartedAt.HasValue)
                    {
                        timeSpent = (int)(now - progress.StartedAt.Value).TotalMinutes;
                    }

                    progress.IsCompleted = true;
                    progress.CompletedAt = now;
                    progress.TimeSpentMinutes = timeSpent;
                    _context.LessonProgresses.Update(progress);
                }
                else
                {
                    progress = new LessonProgress
                    {
                        StudentId = studentId,
                        LessonId = lessonId,
                        IsCompleted = true,
                        StartedAt = now.AddMinutes(-30), // افتراضي
                        CompletedAt = now,
                        TimeSpentMinutes = 30
                    };
                    _context.LessonProgresses.Add(progress);
                }

                await _context.SaveChangesAsync();

                // Check if course is completed
                await CheckCourseCompletion(studentId, lesson.CourseId);

                // Send notification to teacher - التصحيح هنا
                var teacherId = await _context.Courses
                    .Where(c => c.Id == lesson.CourseId)
                    .Select(c => c.Subject.TeacherId)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(teacherId))
                {
                    // استخدام الدالة الصحيحة التي تأخذ teacherId كـ string
                    await SendNotificationToTeacher(lessonId, student, $"{student.FullName} أكمل الدرس: {lesson.Title}");
                }

                // الحصول على التقدم المحدث للدورة
                var totalLessons = await _context.Lessons
                    .CountAsync(l => l.CourseId == lesson.CourseId);

                var completedLessons = await _context.LessonProgresses
                    .CountAsync(lp => lp.StudentId == studentId &&
                                    lp.Lesson.CourseId == lesson.CourseId &&
                                    lp.IsCompleted);

                var courseProgress = totalLessons > 0 ?
                    (int)Math.Round((double)completedLessons / totalLessons * 100) : 0;

                // Update learning streak
                await UpdateLearningStreak(studentId);

                return Json(new
                {
                    success = true,
                    courseProgress = courseProgress,
                    completedLessons = completedLessons,
                    totalLessons = totalLessons,
                    timeSpent = timeSpent,
                    message = "تم إكمال الدرس بنجاح!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إكمال الدرس");
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        private async Task CheckCourseCompletion(string studentId, int courseId)
        {
            var totalLessons = await _context.Lessons
                .CountAsync(l => l.CourseId == courseId);

            var completedLessons = await _context.LessonProgresses
                .CountAsync(lp => lp.StudentId == studentId &&
                                 lp.Lesson.CourseId == courseId &&
                                 lp.IsCompleted);

            if (totalLessons > 0 && completedLessons == totalLessons)
            {
                // Course completed - send achievement notification
                var student = await _userManager.FindByIdAsync(studentId);
                var course = await _context.Courses.FindAsync(courseId);

                // Send notification to student
                var notification = new Notification
                {
                    UserId = studentId,
                    Type = "course_completed",
                    Title = "تهانينا!",
                    Message = $"لقد أكملت دورة {course.Title} بنجاح!",
                    RelatedId = courseId,
                    RelatedType = "course",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
        }

        private async Task UpdateLearningStreak(string studentId)
        {
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            var hasToday = await _context.LessonProgresses
                .AnyAsync(lp => lp.StudentId == studentId &&
                               lp.IsCompleted &&
                               lp.CompletedAt.Value.Date == today);

            var hasYesterday = await _context.LessonProgresses
                .AnyAsync(lp => lp.StudentId == studentId &&
                               lp.IsCompleted &&
                               lp.CompletedAt.Value.Date == yesterday);

            // Streak logic can be implemented here
            if (hasToday)
            {
                // Update streak count in user profile
                var student = await _userManager.FindByIdAsync(studentId);
                // Update streak logic...
            }
        }
        #endregion

        #region Comments System - محسن ومتكامل
        [HttpGet]
        public async Task<IActionResult> GetComments(int lessonId, int page = 1, int pageSize = 10)
        {
            try
            {
                var studentId = await GetCurrentUserId();
                var user = await GetCurrentUser();

                // التحقق من الوصول للدرس
                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.Id == lessonId);

                if (lesson == null || !await IsEnrolledInCourse(lesson.CourseId, studentId))
                {
                    return Json(new { success = false, message = "ليس لديك صلاحية الوصول" });
                }

                // الحصول على التعليقات
                var query = _context.Comments
                    .Include(c => c.User)
                    .Include(c => c.MentionedUser)
                    .Include(c => c.Reactions)
                    .Include(c => c.Replies)
                        .ThenInclude(r => r.User)
                    .Where(c => c.LessonId == lessonId &&
                               !c.IsDeleted &&
                               c.ParentCommentId == null);

                var totalComments = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalComments / pageSize);

                var comments = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // تحويل للـ ViewModel
                var commentViewModels = new List<CommentViewModel>();

                foreach (var comment in comments)
                {
                    var canEdit = comment.UserId == studentId ||
                                 await _userManager.IsInRoleAsync(user, "Teacher") ||
                                 await _userManager.IsInRoleAsync(user, "Admin");

                    var canDelete = canEdit ||
                                   (comment.Replies != null && !comment.Replies.Any());

                    var commentVm = await MapCommentToViewModel(comment, studentId, canEdit, canDelete);
                    commentViewModels.Add(commentVm);
                }

                return Json(new
                {
                    success = true,
                    comments = commentViewModels,
                    totalComments = totalComments,
                    totalPages = totalPages,
                    currentPage = page,
                    hasMore = page < totalPages,
                    lessonTitle = lesson.Title,
                    courseTitle = lesson.Course?.Title
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل التعليقات");
                return Json(new { success = false, message = "حدث خطأ في تحميل التعليقات" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] AddCommentViewModel model)
        {
            try
            {
                var studentId = _userManager.GetUserId(User);
                var student = await _userManager.GetUserAsync(User);

                Console.WriteLine($"Adding comment for lesson {model.LessonId} by {studentId}");

                // تحقق بسيط
                if (string.IsNullOrEmpty(model.Content))
                {
                    return Json(new { success = false, message = "Comment content is required" });
                }

                // إنشاء التعليق
                var comment = new Comment
                {
                    Content = model.Content.Trim(),
                    UserId = studentId,
                    LessonId = model.LessonId,
                    CreatedAt = DateTime.UtcNow,
                    IsEdited = false,
                    IsDeleted = false
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Comment saved with ID: {comment.Id}");

                return Json(new
                {
                    success = true,
                    message = "Comment added successfully",
                    commentId = comment.Id
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(int id, [FromBody] EditCommentViewModel model)
        {
            try
            {
                var studentId = await GetCurrentUserId();
                var user = await GetCurrentUser();

                var comment = await _context.Comments
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (comment == null)
                {
                    return Json(new { success = false, message = "التعليق غير موجود" });
                }

                // التحقق من الصلاحية للتعديل
                var canEdit = comment.UserId == studentId ||
                             await _userManager.IsInRoleAsync(user, "Teacher") ||
                             await _userManager.IsInRoleAsync(user, "Admin");

                if (!canEdit)
                {
                    return Json(new { success = false, message = "ليس لديك صلاحية التعديل" });
                }

                // تحديث التعليق
                comment.Content = model.Content.Trim();
                comment.IsEdited = true;
                comment.UpdatedAt = DateTime.UtcNow;

                _context.Comments.Update(comment);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "تم تعديل التعليق بنجاح",
                    updatedContent = comment.Content
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تعديل التعليق");
                return Json(new { success = false, message = "حدث خطأ في تعديل التعليق" });
            }
        }
        // في ChatController.cs أضف هذه الدوال:

        // API: Get available teachers for chat
        [HttpGet]
        public async Task<IActionResult> GetAvailableTeachers()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                // Get all teachers (excluding current user if they're a teacher)
                var teachers = await _userManager.Users
                    .Where(u => u.IsTeacher && u.Id != currentUser.Id)
                    .Select(t => new Web_Lessons.ViewModels.TeacherViewModel
                    {
                        Id = t.Id,
                        Name = t.FullName,
                        ProfileImageUrl = t.ProfileImageUrl ?? "/images/avatar.png",
                        Email = t.Email,
                        StudentCount = _context.Enrollments
                            .Count(e => e.Course.Subject.TeacherId == t.Id),
                        CourseCount = _context.Courses
                            .Count(c => c.Subject.TeacherId == t.Id)
                    })
                    .Take(20)
                    .ToListAsync();

                return Json(new { success = true, teachers = teachers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available teachers");
                return Json(new { success = false, teachers = new List<TeacherViewModel>() });
            }
        }

        // API: Check if chat exists with teacher
        [HttpGet]
        public async Task<IActionResult> CheckChatWithTeacher(string teacherId)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);

                var existingChat = await _context.Chats
                    .FirstOrDefaultAsync(c =>
                        (c.User1Id == currentUserId && c.User2Id == teacherId) ||
                        (c.User1Id == teacherId && c.User2Id == currentUserId));

                return Json(new
                {
                    success = true,
                    exists = existingChat != null,
                    chatId = existingChat?.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking chat with teacher");
                return Json(new { success = false, exists = false });
            }
        }

        // In StudentController.cs - REMOVE the duplicate method definition
        // Keep only ONE MapCommentToViewModel method

        private async Task<CommentViewModel> MapCommentToViewModel(Comment comment, string currentUserId,
            bool canEdit, bool canDelete)
        {
            // الحصول على التفاعلات
            var reactions = await _context.CommentReactions
                .Where(cr => cr.CommentId == comment.Id)
                .GroupBy(cr => cr.ReactionType)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Count()
                );

            // الحصول على تفاعل المستخدم الحالي
            var currentUserReaction = await _context.CommentReactions
                .Where(cr => cr.CommentId == comment.Id && cr.UserId == currentUserId)
                .Select(cr => cr.ReactionType)
                .FirstOrDefaultAsync();

            // حساب عدد الردود
            var repliesCount = await _context.Comments
                .CountAsync(c => c.ParentCommentId == comment.Id && !c.IsDeleted);

            // حساب TimeAgo
            var timeAgo = GetTimeAgo(comment.CreatedAt);

            var commentVm = new CommentViewModel
            {
                Id = comment.Id,
                Content = comment.Content,
                UserId = comment.UserId,
                UserName = comment.User?.FullName ?? "مستخدم غير معروف",
                UserProfileImage = comment.User?.ProfileImageUrl ?? "/images/default-avatar.png",
                IsTeacher = comment.User?.IsTeacher ?? false,
                ParentCommentId = comment.ParentCommentId,
                MentionedUserId = comment.MentionedUserId,
                MentionedUserName = comment.MentionedUser?.FullName,
                IsEdited = comment.IsEdited,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                TimeAgo = timeAgo,
                RepliesCount = repliesCount,
                Reactions = reactions,
                CurrentUserReaction = currentUserReaction,
                CanEdit = canEdit,
                CanDelete = canDelete,
                LessonId = comment.LessonId,
                LessonTitle = comment.Lesson?.Title
            };

            // إضافة الردود إذا كان هناك
            if (repliesCount > 0)
            {
                var replies = await _context.Comments
                    .Include(c => c.User)
                    .Include(c => c.MentionedUser)
                    .Include(c => c.Reactions)
                    .Where(c => c.ParentCommentId == comment.Id && !c.IsDeleted)
                    .OrderBy(c => c.CreatedAt)
                    .Take(5) // Load only 5 replies initially
                    .ToListAsync();

                foreach (var reply in replies)
                {
                    var canEditReply = reply.UserId == currentUserId || canEdit;
                    var canDeleteReply = canEditReply;

                    var replyVm = await MapCommentToViewModel(reply, currentUserId, canEditReply, canDeleteReply);
                    commentVm.Replies.Add(replyVm);
                }
            }

            return commentVm;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                var studentId = await GetCurrentUserId();
                var user = await GetCurrentUser();

                var comment = await _context.Comments
                    .Include(c => c.Replies)
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (comment == null)
                {
                    return Json(new { success = false, message = "التعليق غير موجود" });
                }

                // التحقق من الصلاحية للحذف
                var canDelete = comment.UserId == studentId ||
                               await _userManager.IsInRoleAsync(user, "Teacher") ||
                               await _userManager.IsInRoleAsync(user, "Admin");

                if (!canDelete)
                {
                    return Json(new { success = false, message = "ليس لديك صلاحية الحذف" });
                }

                // إذا كان هناك ردود، نضع محتوى "تم الحذف"
                if (comment.Replies != null && comment.Replies.Any())
                {
                    comment.IsDeleted = true;
                    comment.Content = "[تم حذف التعليق]";
                    _context.Comments.Update(comment);
                }
                else
                {
                    _context.Comments.Remove(comment);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تم حذف التعليق بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف التعليق");
                return Json(new { success = false, message = "حدث خطأ في حذف التعليق" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReaction(int commentId, string reactionType)
        {
            try
            {
                var studentId = await GetCurrentUserId();

                var comment = await _context.Comments
                    .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

                if (comment == null)
                {
                    return Json(new { success = false, message = "التعليق غير موجود" });
                }

                // التحقق من نوع التفاعل
                var validReactions = new[] { "like", "love", "haha", "wow", "sad", "angry" };
                if (!validReactions.Contains(reactionType.ToLower()))
                {
                    return Json(new { success = false, message = "نوع التفاعل غير صالح" });
                }

                // التحقق من وجود تفاعل سابق
                var existingReaction = await _context.CommentReactions
                    .FirstOrDefaultAsync(cr => cr.CommentId == commentId && cr.UserId == studentId);

                if (existingReaction != null)
                {
                    // إذا كان نفس التفاعل، إزالته
                    if (existingReaction.ReactionType == reactionType)
                    {
                        _context.CommentReactions.Remove(existingReaction);
                    }
                    else
                    {
                        // تحديث التفاعل
                        existingReaction.ReactionType = reactionType;
                        existingReaction.CreatedAt = DateTime.UtcNow;
                        _context.CommentReactions.Update(existingReaction);
                    }
                }
                else
                {
                    // إضافة تفاعل جديد
                    var reaction = new CommentReaction
                    {
                        UserId = studentId,
                        CommentId = commentId,
                        ReactionType = reactionType,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CommentReactions.Add(reaction);

                    // إرسال إشعار لصاحب التعليق
                    if (comment.UserId != studentId)
                    {
                        var user = await GetCurrentUser();
                        var notification = new Notification
                        {
                            UserId = comment.UserId,
                            Type = "reaction",
                            Title = "تفاعل جديد",
                            Message = $"{user.FullName} تفاعل مع تعليقك",
                            RelatedId = commentId,
                            RelatedType = "comment",
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Notifications.Add(notification);
                    }
                }

                await _context.SaveChangesAsync();

                // الحصول على إحصائيات التفاعلات المحدثة
                var reactions = await _context.CommentReactions
                    .Where(cr => cr.CommentId == commentId)
                    .GroupBy(cr => cr.ReactionType)
                    .Select(g => new
                    {
                        ReactionType = g.Key,
                        Count = g.Count(),
                        IsCurrentUserReacted = g.Any(r => r.UserId == studentId)
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    reactions = reactions,
                    message = "تم تحديث التفاعل بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إضافة التفاعل");
                return Json(new { success = false, message = "حدث خطأ في إضافة التفاعل" });
            }
        }



        // يجب أن تكون هذه الدالة موجودة في StudentController.cs
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
        #endregion

        #region Profile & Settings
        public async Task<IActionResult> Profile()
        {
            var student = await GetCurrentUser();
            if (student == null)
            {
                return NotFound();
            }

            var studentId = student.Id;

            // الحصول على الإحصائيات
            var enrolledCoursesCount = await _context.Enrollments
                .CountAsync(e => e.StudentId == studentId);

            var completedLessonsCount = await _context.LessonProgresses
                .CountAsync(lp => lp.StudentId == studentId && lp.IsCompleted);

            var totalCoursesCount = await _context.Courses
                .CountAsync(c => c.IsPublished);

            var allLessonsCount = await _context.Lessons
                .CountAsync();

            var completedPercent = allLessonsCount > 0 ?
                (int)Math.Round((double)completedLessonsCount / allLessonsCount * 100) : 0;

            // الحصول على الدورات الأخيرة
            var recentCourses = await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == studentId)
                .OrderByDescending(e => e.EnrolledAt)
                .Take(3)
                .Select(e => e.Course)
                .ToListAsync();

            // الحصول على الإنجازات الأخيرة
            var recentAchievements = await _context.LessonProgresses
                .Include(lp => lp.Lesson)
                .ThenInclude(l => l.Course)
                .Where(lp => lp.StudentId == studentId && lp.IsCompleted)
                .OrderByDescending(lp => lp.CompletedAt)
                .Take(5)
                .Select(lp => new AchievementViewModel
                {
                    LessonTitle = lp.Lesson.Title,
                    CourseTitle = lp.Lesson.Course.Title,
                    CompletedAt = lp.CompletedAt.Value,
                    TimeSpent = lp.TimeSpentMinutes ?? 0
                })
                .ToListAsync();

            var model = new StudentProfileViewModel
            {
                User = student,
                JoinDate = student.CreatedOn,
                LastLogin = student.LastLogin,
                EnrolledCoursesCount = enrolledCoursesCount,
                CompletedLessonsCount = completedLessonsCount,
                TotalCoursesCount = totalCoursesCount,
                OverallProgress = completedPercent,
                RecentCourses = recentCourses,
                RecentAchievements = recentAchievements,
                TotalLearningTime = await CalculateTotalLearningTime(studentId)
            };

            return View(model);
        }

        private async Task<string> CalculateTotalLearningTime(string studentId)
        {
            var totalMinutes = await _context.LessonProgresses
                .Where(lp => lp.StudentId == studentId && lp.IsCompleted)
                .SumAsync(lp => lp.TimeSpentMinutes ?? 0);

            var hours = totalMinutes / 60;
            var minutes = totalMinutes % 60;

            return $"{hours} ساعة و {minutes} دقيقة";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserProfileViewModel model)
        {
            try
            {
                var student = await GetCurrentUser();
                if (student == null)
                {
                    return Json(new { success = false, message = "المستخدم غير موجود" });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "بيانات غير صالحة", errors });
                }

                // تحديث البيانات
                student.FullName = model.FullName?.Trim();
                student.Bio = model.Bio?.Trim();

                // تحديث صورة الملف الشخصي
                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile-images");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ProfileImage.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfileImage.CopyToAsync(stream);
                    }

                    student.ProfileImageUrl = $"/uploads/profile-images/{uniqueFileName}";
                }

                var result = await _userManager.UpdateAsync(student);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = "فشل تحديث الملف الشخصي", errors });
                }

                return Json(new
                {
                    success = true,
                    message = "تم تحديث الملف الشخصي بنجاح",
                    profileImageUrl = student.ProfileImageUrl,
                    fullName = student.FullName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديث الملف الشخصي");
                return Json(new { success = false, message = "حدث خطأ في تحديث الملف الشخصي" });
            }
        }
        #endregion

        #region Notifications
        [HttpGet]
        public async Task<IActionResult> GetNotifications(int page = 1, int pageSize = 20)
        {
            try
            {
                var studentId = await GetCurrentUserId();

                var query = _context.Notifications
                    .Where(n => n.UserId == studentId)
                    .OrderByDescending(n => n.CreatedAt);

                var totalNotifications = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalNotifications / pageSize);

                var notifications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new NotificationViewModel
                    {
                        Id = n.Id,
                        Type = n.Type,
                        Title = n.Title,
                        Message = n.Message,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt,
                        TimeAgo = GetTimeAgo(n.CreatedAt),
                        RelatedId = n.RelatedId,
                        RelatedType = n.RelatedType
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    notifications = notifications,
                    totalNotifications = totalNotifications,
                    totalPages = totalPages,
                    currentPage = page,
                    unreadCount = await _context.Notifications.CountAsync(n => n.UserId == studentId && !n.IsRead)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل الإشعارات");
                return Json(new { success = false, message = "حدث خطأ في تحميل الإشعارات" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                var studentId = await GetCurrentUserId();

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == studentId);

                if (notification == null)
                {
                    return Json(new { success = false, message = "الإشعار غير موجود" });
                }

                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                    _context.Notifications.Update(notification);
                    await _context.SaveChangesAsync();
                }

                var unreadCount = await _context.Notifications
                    .CountAsync(n => n.UserId == studentId && !n.IsRead);

                return Json(new
                {
                    success = true,
                    message = "تم وضع علامة مقروء",
                    unreadCount = unreadCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في وضع علامة مقروء على الإشعار");
                return Json(new { success = false, message = "حدث خطأ في تحديث الإشعار" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, unreadCount = 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return Json(new { success = false, message = "Error marking all as read" });
            }
        }
        #endregion

        #region Chat
        [HttpGet]
        public async Task<IActionResult> GetChats()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);

                // Get all chats for current user
                var chats = await _context.Chats
                    .Include(c => c.User1)
                    .Include(c => c.User2)
                    .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                    .Where(c => c.User1Id == userId || c.User2Id == userId)
                    .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                var chatViewModels = new List<ChatViewModel>();
                foreach (var chat in chats)
                {
                    var otherUser = chat.User1Id == userId ? chat.User2 : chat.User1;
                    var lastMessage = chat.Messages?.FirstOrDefault();
                    var unreadCount = await _context.ChatMessages
                        .CountAsync(m => m.ChatId == chat.Id &&
                                        m.SenderId != userId &&
                                        !m.IsRead);

                    chatViewModels.Add(new ChatViewModel
                    {
                        Id = chat.Id,
                        OtherUserId = otherUser.Id,
                        OtherUserName = otherUser.FullName,
                        OtherUserProfileImage = otherUser.ProfileImageUrl ?? "/images/avatar.png",
                        IsTeacher = otherUser.IsTeacher,
                        LastMessage = lastMessage?.Content,
                        LastMessageAt = lastMessage?.CreatedAt,
                        UnreadCount = unreadCount,
                        IsOnline = false // We'll implement this later with SignalR
                    });
                }

                return Json(new { success = true, chats = chatViewModels });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chats");
                return Json(new { success = false, chats = new List<ChatViewModel>() });
            }
        }
        #endregion

        [HttpGet]
        public async Task<IActionResult> GetAvailableLessons()
        {
            try
            {
                var studentId = await GetCurrentUserId();

                // Get all enrolled courses
                var enrolledCourseIds = await _context.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .Select(e => e.CourseId)
                    .ToListAsync();

                // Get lessons from enrolled courses that are not completed
                var availableLessons = await _context.Lessons
                    .Include(l => l.Course)
                    .Where(l => enrolledCourseIds.Contains(l.CourseId))
                    .OrderBy(l => l.CourseId)
                    .ThenBy(l => l.Order)
                    .Select(l => new
                    {
                        id = l.Id,
                        title = l.Title,
                        description = l.Description,
                        duration = l.DurationMinutes,
                        order = l.Order,
                        courseId = l.CourseId,
                        courseTitle = l.Course.Title,
                        isCompleted = _context.LessonProgresses
                            .Any(lp => lp.StudentId == studentId &&
                                       lp.LessonId == l.Id &&
                                       lp.IsCompleted)
                    })
                    .Where(l => !l.isCompleted)
                    .ToListAsync();

                // Convert to view model
                var lessons = availableLessons.Select(l => new
                {
                    id = l.id,
                    title = l.title,
                    courseTitle = l.courseTitle,
                    duration = l.duration,
                    order = l.order,
                    link = $"/Student/LessonDetails/{l.id}",
                    timeAgo = GetTimeAgo(DateTime.UtcNow) // You can store lesson created date if needed
                }).ToList();

                return Json(new
                {
                    success = true,
                    lessons = lessons,
                    count = lessons.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available lessons");
                return Json(new
                {
                    success = false,
                    message = "Error loading lessons"
                });
            }
        }
        // In StudentController.cs - Add these new methods

        #region Enhanced Comments System
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportComment(int commentId, string reason)
        {
            try
            {
                var studentId = await GetCurrentUserId();

                var comment = await _context.Comments
                    .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

                if (comment == null)
                    return Json(new { success = false, message = "التعليق غير موجود" });

                // Check if already reported by this user
                var existingReport = await _context.CommentReports
                    .FirstOrDefaultAsync(cr => cr.CommentId == commentId && cr.ReporterId == studentId);

                if (existingReport != null)
                    return Json(new { success = false, message = "لقد أبلغت عن هذا التعليق مسبقاً" });

                // Create report
                var report = new CommentReport
                {
                    CommentId = commentId,
                    ReporterId = studentId,
                    Reason = reason,
                    ReportedAt = DateTime.UtcNow
                };

                _context.CommentReports.Add(report);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تم الإبلاغ عن التعليق بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting comment");
                return Json(new { success = false, message = "حدث خطأ أثناء الإبلاغ" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsersForMention(string search)
        {
            try
            {
                var currentUserId = await GetCurrentUserId();

                var users = await _userManager.Users
                    .Where(u => u.Id != currentUserId &&
                           (u.FullName.Contains(search) || u.Email.Contains(search)))
                    .Select(u => new
                    {
                        id = u.Id,
                        name = u.FullName,
                        email = u.Email,
                        isTeacher = u.IsTeacher,
                        profileImage = u.ProfileImageUrl ?? "/images/default-avatar.png"
                    })
                    .Take(10)
                    .ToListAsync();

                return Json(new { success = true, users = users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users");
                return Json(new { success = false, message = "Error searching users" });
            }
        }
        #endregion

        #region Enhanced Chat System

        // في StudentController.cs - إصلاح دالة GetUnreadMessagesCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadMessagesCount()
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                // حساب الرسائل غير المقروءة بشكل صحيح
                var unreadCount = await _context.ChatMessages
                    .Where(m =>
                        (m.Chat.User1Id == userId || m.Chat.User2Id == userId) &&
                        m.SenderId != userId &&
                        !m.IsRead)
                    .CountAsync();

                return Json(new { success = true, count = unreadCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread messages count");
                return Json(new { success = false, count = 0 });
            }
        }

        // إضافة دالة جديدة للحصول على جميع رسائل الشات

        [HttpPost]
        public async Task<IActionResult> MarkMessagesAsRead(int chatId)
        {
            try
            {
                var studentId = await GetCurrentUserId();

                var unreadMessages = await _context.ChatMessages
                    .Where(m => m.ChatId == chatId &&
                               m.SenderId != studentId &&
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
                _logger.LogError(ex, "Error marking messages as read");
                return Json(new { success = false });
            }
        }
        #endregion
        private async Task CreateSampleNotifications(string userId)
        {
            try
            {
                var sampleNotifications = new List<Notification>
                {
                    new Notification
                    {
                        UserId = userId,
                        Type = "system",
                        Title = "Welcome to Web Lessons!",
                        Message = "Start exploring courses and begin your learning journey.",
                        RelatedType = "dashboard",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-2)
                    },
                    new Notification
                    {
                        UserId = userId,
                        Type = "enrollment",
                        Title = "Course Enrollment Available",
                        Message = "New courses have been added. Check them out!",
                        RelatedType = "course",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-5)
                    },
                    new Notification
                    {
                        UserId = userId,
                        Type = "system",
                        Title = "Daily Learning Goal",
                        Message = "Complete 60 minutes of learning today to reach your goal.",
                        RelatedType = "dashboard",
                        IsRead = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    }
                };

                await _context.Notifications.AddRangeAsync(sampleNotifications);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sample notifications");
            }
        }
        // في StudentController.cs - إضافة هذه الدوال الجديدة
        [HttpGet]
        public async Task<IActionResult> GetDashboardNotifications()
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                // Get notifications from last 7 days only
                var lastWeek = DateTime.UtcNow.AddDays(-7);

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && n.CreatedAt >= lastWeek)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(20) // Limit to 20 notifications
                    .Select(n => new NotificationViewModel
                    {
                        Id = n.Id,
                        Type = n.Type,
                        Title = n.Title,
                        Message = n.Message,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt,
                        TimeAgo = GetTimeAgo(n.CreatedAt),
                        RelatedId = n.RelatedId,
                        RelatedType = n.RelatedType
                    })
                    .ToListAsync();

                var unreadCount = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead && n.CreatedAt >= lastWeek);

                return Json(new
                {
                    success = true,
                    notifications = notifications,
                    unreadCount = unreadCount,
                    hasMore = await _context.Notifications
                        .CountAsync(n => n.UserId == userId) > 20
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard notifications");
                return Json(new
                {
                    success = false,
                    message = "Error loading notifications",
                    notifications = new List<NotificationViewModel>(),
                    unreadCount = 0
                });
            }
        }

         [HttpPost]
        public async Task<IActionResult> GenerateTestNotification()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                var notification = new Notification
                {
                    UserId = userId,
                    Type = "system",
                    Title = "Welcome to Web Lessons!",
                    Message = $"Hello {user.FullName}, welcome to our learning platform. Start your journey now!",
                    RelatedType = "dashboard",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test notification");
                return Json(new { success = false });
            }
        }

// في داخل الـ StudentController أضف هذه الدوال:

[HttpGet]
    public async Task<IActionResult> GetTeacherOnlineStatus(string teacherId)
    {
        try
        {
            var isOnline = ChatHub.IsUserOnline(teacherId);
            var lastSeen = "Recently";

            if (isOnline)
            {
                lastSeen = "Now";
            }
            else
            {
                var teacher = await _userManager.FindByIdAsync(teacherId);
                if (teacher?.LastLogin != null)
                {
                    var timeDiff = DateTime.UtcNow - teacher.LastLogin.Value;
                    if (timeDiff.TotalMinutes < 5) lastSeen = "Just now";
                    else if (timeDiff.TotalHours < 1) lastSeen = $"{(int)timeDiff.TotalMinutes} min ago";
                    else if (timeDiff.TotalDays < 1) lastSeen = $"{(int)timeDiff.TotalHours} hours ago";
                    else lastSeen = teacher.LastLogin.Value.ToString("MMM dd");
                }
            }

            return Json(new
            {
                success = true,
                isOnline = isOnline,
                lastSeen = lastSeen
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting teacher online status");
            return Json(new
            {
                success = false,
                message = "Error getting status"
            });
        }
    }

        [HttpGet]
        public async Task<IActionResult> GetCourseLessons(int courseId)
        {
            try
            {
                var studentId = await GetCurrentUserId();

                var lessons = await _context.Lessons
                    .Where(l => l.CourseId == courseId)
                    .OrderBy(l => l.Order)
                    .Select(l => new
                    {
                        id = l.Id,
                        title = l.Title,
                        duration = l.DurationMinutes,
                        order = l.Order,
                        isCompleted = _context.LessonProgresses
                            .Any(lp => lp.StudentId == studentId &&
                                      lp.LessonId == l.Id &&
                                      lp.IsCompleted)
                    })
                    .ToListAsync();

                return Json(new { success = true, lessons = lessons });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course lessons");
                return Json(new { success = false });
            }
        }

        // في StudentController.cs - تحديث إجراء StartChatWithTeacher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartChatWithTeacher(
            string teacherId,
            string courseId = null,
            string initialMessage = null)
        {
            try
            {
                var studentId = _userManager.GetUserId(User);
                var student = await _userManager.GetUserAsync(User);

                // التحقق من أن المستخدم طالب
                if (student.IsTeacher)
                {
                    return Json(new { success = false, message = "Only students can start chats" });
                }

                // التحقق من أن المعلم موجود
                var teacher = await _userManager.FindByIdAsync(teacherId);
                if (teacher == null || !teacher.IsTeacher)
                {
                    return Json(new { success = false, message = "Teacher not found" });
                }

                // التحقق من وجود محادثة سابقة
                var existingChat = await _context.Chats
                    .FirstOrDefaultAsync(c =>
                        (c.User1Id == studentId && c.User2Id == teacherId) ||
                        (c.User1Id == teacherId && c.User2Id == studentId));

                if (existingChat != null)
                {
                    return Json(new
                    {
                        success = true,
                        chatId = existingChat.Id,
                        message = "Chat already exists",
                        redirect = Url.Action("Details", "Chat", new { id = existingChat.Id })
                    });
                }

                // إنشاء محادثة جديدة
                var chat = new Chat
                {
                    User1Id = studentId,
                    User2Id = teacherId,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = null
                };

                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();

                // إضافة رسالة أولية إذا كانت موجودة
                if (!string.IsNullOrEmpty(initialMessage))
                {
                    var message = new ChatMessage
                    {
                        ChatId = chat.Id,
                        SenderId = studentId,
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
                    message = "Chat created successfully",
                    redirect = Url.Action("Details", "Chat", new { id = chat.Id })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StartChatWithTeacher");
                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                });
            }
        }


        #region Enhanced Progress Tracking
        [HttpGet]
        public async Task<IActionResult> GetCourseProgressChart(int courseId)
        {
            try
            {
                var studentId = await GetCurrentUserId();

                var lessons = await _context.Lessons
                    .Where(l => l.CourseId == courseId)
                    .OrderBy(l => l.Order)
                    .Select(l => new
                    {
                        id = l.Id,
                        title = l.Title,
                        order = l.Order
                    })
                    .ToListAsync();

                var progressData = new List<object>();

                foreach (var lesson in lessons)
                {
                    var isCompleted = await _context.LessonProgresses
                        .AnyAsync(lp => lp.StudentId == studentId &&
                                      lp.LessonId == lesson.id &&
                                      lp.IsCompleted);

                    progressData.Add(new
                    {
                        lesson = lesson.title,
                        order = lesson.order,
                        completed = isCompleted
                    });
                }

                return Json(new { success = true, data = progressData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting progress chart");
                return Json(new { success = false });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLearningStats(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var studentId = await GetCurrentUserId();

                var query = _context.LessonProgresses
                    .Where(lp => lp.StudentId == studentId && lp.IsCompleted);

                if (startDate.HasValue)
                    query = query.Where(lp => lp.CompletedAt >= startDate);

                if (endDate.HasValue)
                    query = query.Where(lp => lp.CompletedAt <= endDate);

                var stats = await query
                    .GroupBy(lp => lp.CompletedAt.Value.Date)
                    .Select(g => new
                    {
                        date = g.Key,
                        lessonsCompleted = g.Count(),
                        timeSpent = g.Sum(lp => lp.TimeSpentMinutes ?? 0)
                    })
                    .OrderBy(s => s.date)
                    .ToListAsync();

                return Json(new { success = true, stats = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting learning stats");
                return Json(new { success = false });
            }
        }
        #endregion

        #region Enhanced Course Enrollment
        [HttpPost]
        public async Task<IActionResult> Unenroll(int courseId)
        {
            try
            {
                var studentId = await GetCurrentUserId();

                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);

                if (enrollment == null)
                    return Json(new { success = false, message = "You are not enrolled in this course" });

                _context.Enrollments.Remove(enrollment);

                // Remove all progress for this course
                var progresses = await _context.LessonProgresses
                    .Include(lp => lp.Lesson)
                    .Where(lp => lp.StudentId == studentId && lp.Lesson.CourseId == courseId)
                    .ToListAsync();

                _context.LessonProgresses.RemoveRange(progresses);

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Successfully unenrolled from course"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unenrolling from course");
                return Json(new { success = false, message = "Error unenrolling" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecommendedCourses()
        {
            try
            {
                var studentId = await GetCurrentUserId();

                // Get courses the student is enrolled in
                var enrolledCourseIds = await _context.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .Select(e => e.CourseId)
                    .ToListAsync();

                // Get courses in same subjects as enrolled courses
                var enrolledSubjects = await _context.Enrollments
                    .Include(e => e.Course)
                    .Where(e => e.StudentId == studentId)
                    .Select(e => e.Course.SubjectId)
                    .Distinct()
                    .ToListAsync();

                var recommendedCourses = await _context.Courses
                    .Include(c => c.Subject)
                    .Include(c => c.Enrollments)
                    .Where(c => c.IsPublished &&
                               !enrolledCourseIds.Contains(c.Id) &&
                               enrolledSubjects.Contains(c.SubjectId))
                    .OrderByDescending(c => c.Enrollments.Count)
                    .Take(6)
                    .Select(c => new
                    {
                        id = c.Id,
                        title = c.Title,
                        description = c.Description,
                        thumbnail = c.ThumbnailUrl ?? "/images/default-course.jpg",
                        level = c.Level,
                        subject = c.Subject.Name,
                        studentCount = c.Enrollments.Count,
                        lessonCount = c.Lessons.Count
                    })
                    .ToListAsync();

                return Json(new { success = true, courses = recommendedCourses });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommended courses");
                return Json(new { success = false, courses = new List<object>() });
            }
        }
        #endregion

        #region Enhanced Notifications System
        [HttpGet]
        public async Task<IActionResult> GetNotificationSettings()
        {
            try
            {
                var studentId = await GetCurrentUserId();

                // In a real app, you'd have a NotificationSettings table
                var settings = new
                {
                    emailNotifications = true,
                    pushNotifications = true,
                    lessonCompleted = true,
                    newComment = true,
                    mention = true,
                    reaction = true,
                    newMessage = true
                };

                return Json(new { success = true, settings = settings });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification settings");
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateNotificationSettings([FromBody] Dictionary<string, bool> settings)
        {
            try
            {
                // In a real app, you'd save these to a database
                return Json(new
                {
                    success = true,
                    message = "Notification settings updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification settings");
                return Json(new { success = false, message = "Error updating settings" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ClearAllNotifications()
        {
            try
            {
                var studentId = await GetCurrentUserId();

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == studentId)
                    .ToListAsync();

                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "All notifications cleared"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing notifications");
                return Json(new { success = false, message = "Error clearing notifications" });
            }
        }
        #endregion
        #region Helper Methods
        private async Task UpdateLastActivity(string userId, string type, string description, int? relatedId = null)
        {
            try
            {
                // يمكن حفظ النشاط في جدول منفصل إذا لزم الأمر
                // حالياً نكتفي بتسجيل في الـ Log
                _logger.LogInformation($"User Activity: {userId} - {type} - {description}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تسجيل النشاط");
            }
        }
        #endregion
    }
}