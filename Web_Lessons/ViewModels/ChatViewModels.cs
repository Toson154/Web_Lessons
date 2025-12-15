using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Web_Lessons.ViewModels
{
    public class CreateChatViewModel
    {
        [Required]
        public string TeacherId { get; set; }

        [MaxLength(1000, ErrorMessage = "Message cannot exceed 1000 characters")]
        public string InitialMessage { get; set; }

        public int? LessonId { get; set; }
        public string ReturnUrl { get; set; } = "/Chat";
    }

    public class SendMessageViewModel
    {
        [Required]
        public int ChatId { get; set; }

        [Required]
        [MaxLength(2000, ErrorMessage = "Message cannot exceed 2000 characters")]
        public string Content { get; set; }

        public IFormFile Attachment { get; set; }
        public string ReturnUrl { get; set; } = "/Chat";
    }
    // في ChatViewModels.cs - تحديث ChatViewModel
    public class ChatListViewModel
    {
        public int Id { get; set; }
        public string OtherUserId { get; set; }
        public string OtherUserName { get; set; }
        public string? OtherUserProfileImage { get; set; }
        public bool IsTeacher { get; set; }
        public string LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public bool IsOnline { get; set; }
        public DateTime ChatCreatedAt { get; set; }
    }

    public class ChatViewModel
    {
        public int Id { get; set; }
        public string OtherUserId { get; set; }
        public string OtherUserName { get; set; }
        public string? OtherUserProfileImage { get; set; }
        public bool IsTeacher { get; set; }
        public string LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public bool IsOnline { get; set; }
        public DateTime CreatedAt { get; set; } // أضف هذا السطر
    }
    public class ChatMessageViewModel
    {
        public int Id { get; set; }
        public int ChatId { get; set; } // أضف هذا السطر
        public string Content { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string SenderProfileImage { get; set; }
        public bool IsOwnMessage { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
        public string AttachmentUrl { get; set; }
        public string AttachmentType { get; set; }
    }    // ViewModel للإشعار
    public class LessonNotificationViewModel
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
        public int? RelatedId { get; set; }
        public string RelatedType { get; set; }

        // بيانات إضافية حسب نوع الإشعار
        public string CommentContent { get; set; }
        public string LessonTitle { get; set; }
        public string CourseTitle { get; set; }
        public string ReactionType { get; set; }
        public string MentionedContent { get; set; }
    }
    public class ChatIndexViewModel
    {
        public List<ChatViewModel> Chats { get; set; } = new List<ChatViewModel>();
        public List<TeacherViewModel> AvailableTeachers { get; set; } = new List<TeacherViewModel>();
        public CreateChatViewModel NewChat { get; set; } = new CreateChatViewModel();
    }
    public class ChatDetailViewModel
    {
        public ChatViewModel Chat { get; set; }
        public List<ChatMessageViewModel> Messages { get; set; } = new List<ChatMessageViewModel>();
        public SendMessageViewModel NewMessage { get; set; } = new SendMessageViewModel();
    }
    public class ChatListResponseViewModel
    {
        public List<ChatViewModel> Chats { get; set; } = new List<ChatViewModel>();
        public int TotalUnread { get; set; }
    }
}