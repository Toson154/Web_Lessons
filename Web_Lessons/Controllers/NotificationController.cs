// إنشاء ملف جديد: Controllers/NotificationController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Web_Lessons.Models;
using Web_Lessons.ViewModels;

namespace Web_Lessons.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/notification
        [HttpGet]
        public async Task<ActionResult<NotificationResponse>> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool unreadOnly = false)
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                var query = _context.Notifications
                    .Where(n => n.UserId == userId);

                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        Type = n.Type,
                        Title = n.Title,
                        Message = n.Message,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt,
                        ReadAt = n.ReadAt,
                        RelatedId = n.RelatedId,
                        RelatedType = n.RelatedType,
                        TimeAgo = GetTimeAgo(n.CreatedAt)
                    })
                    .ToListAsync();

                var unreadCount = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                return Ok(new NotificationResponse
                {
                    Notifications = notifications,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    UnreadCount = unreadCount,
                    HasMore = page < totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return StatusCode(500, new { error = "Error loading notifications" });
            }
        }

        // GET: api/notification/count
        [HttpGet("count")]
        public async Task<ActionResult> GetNotificationCount()
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                var count = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                return Ok(new { count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification count");
                return Ok(new { count = 0 });
            }
        }

        // PUT: api/notification/{id}/read
        [HttpPut("{id}/read")]
        public async Task<ActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                {
                    return NotFound(new { error = "Notification not found" });
                }

                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                    _context.Notifications.Update(notification);
                    await _context.SaveChangesAsync();
                }

                var unreadCount = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                return Ok(new
                {
                    success = true,
                    unreadCount = unreadCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, new { error = "Error updating notification" });
            }
        }

        // PUT: api/notification/read-all
        [HttpPut("read-all")]
        public async Task<ActionResult> MarkAllAsRead()
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

                return Ok(new
                {
                    success = true,
                    count = unreadNotifications.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { error = "Error updating notifications" });
            }
        }

        // DELETE: api/notification/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNotification(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                {
                    return NotFound(new { error = "Notification not found" });
                }

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                var unreadCount = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                return Ok(new
                {
                    success = true,
                    unreadCount = unreadCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return StatusCode(500, new { error = "Error deleting notification" });
            }
        }

        // POST: api/notification/generate-test
        [HttpPost("generate-test")]
        public async Task<ActionResult> GenerateTestNotification()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                var notification = new Notification
                {
                    UserId = userId,
                    Type = "system",
                    Title = "Test Notification",
                    Message = $"Hello {user.FullName}, this is a test notification!",
                    RelatedType = "dashboard",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    notificationId = notification.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test notification");
                return StatusCode(500, new { error = "Error generating test notification" });
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
    }

    // DTO Classes
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public int? RelatedId { get; set; }
        public string RelatedType { get; set; }
        public string TimeAgo { get; set; }
    }

    public class NotificationResponse
    {
        public List<NotificationDto> Notifications { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int UnreadCount { get; set; }
        public bool HasMore { get; set; }
    }
}