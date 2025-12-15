    namespace Web_Lessons.Models;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ========== DbSets ==========
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentReaction> CommentReactions { get; set; }
        public DbSet<CommentReply> CommentReplies { get; set; }
        public DbSet<CommentReport> CommentReports { get; set; }
        public DbSet<LessonProgress> LessonProgresses { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<LessonNote> LessonNotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== ApplicationUser Relationships ==========
            // ApplicationUser -> Subjects (Teacher)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Subjects)
                .WithOne(s => s.Teacher)
                .HasForeignKey(s => s.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // ApplicationUser -> Enrollments (Student)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Enrollments)
                .WithOne(e => e.Student)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ApplicationUser -> LessonProgresses
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.LessonProgresses)
                .WithOne(lp => lp.Student)
                .HasForeignKey(lp => lp.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ApplicationUser -> Comments
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Comments)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ApplicationUser -> Notifications
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== Chat Relationships ==========
            // Chat -> User1 (Student)
            modelBuilder.Entity<Chat>()
                .HasOne(c => c.User1)
                .WithMany()
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Chat -> User2 (Teacher)
            modelBuilder.Entity<Chat>()
                .HasOne(c => c.User2)
                .WithMany()
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Chat -> Messages
            modelBuilder.Entity<Chat>()
                .HasMany(c => c.Messages)
                .WithOne(m => m.Chat)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatMessage -> Sender
            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.Sender)
                .WithMany()
                .HasForeignKey(cm => cm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== Comment Relationships ==========
            // Comment -> User
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comment -> Lesson
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Lesson)
                .WithMany(l => l.Comments)
                .HasForeignKey(c => c.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Comment -> ParentComment (for replies)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Comment -> MentionedUser ✅ أضف هذا السطر قبل تعريف العلاقة
            modelBuilder.Entity<Comment>()
                .Property(c => c.MentionedUserId)
                .HasMaxLength(450); // ⭐⭐ هذا هو السطر المفقود! ⭐⭐

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.MentionedUser)
                .WithMany()
                .HasForeignKey(c => c.MentionedUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Comment -> IsDeleted
            modelBuilder.Entity<Comment>()
                .Property(c => c.IsDeleted)
                .HasDefaultValue(false);

            // ========== CommentReaction Relationships ==========
            modelBuilder.Entity<CommentReaction>()
                .HasOne(cr => cr.User)
                .WithMany()
                .HasForeignKey(cr => cr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommentReaction>()
                .HasOne(cr => cr.Comment)
                .WithMany(c => c.Reactions)
                .HasForeignKey(cr => cr.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommentReaction>()
                .HasIndex(cr => new { cr.UserId, cr.CommentId })
                .IsUnique();

            // ========== CommentReply Relationships ==========
            modelBuilder.Entity<CommentReply>()
                .HasOne(cr => cr.Comment)
                .WithMany()
                .HasForeignKey(cr => cr.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommentReply>()
                .HasOne(cr => cr.User)
                .WithMany()
                .HasForeignKey(cr => cr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== CommentReport Relationships ==========
            modelBuilder.Entity<CommentReport>()
                .HasOne(cr => cr.Comment)
                .WithMany()
                .HasForeignKey(cr => cr.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommentReport>()
                .HasOne(cr => cr.Reporter)
                .WithMany()
                .HasForeignKey(cr => cr.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== Subject Relationships ==========
            modelBuilder.Entity<Subject>()
                .HasMany(s => s.Courses)
                .WithOne(c => c.Subject)
                .HasForeignKey(c => c.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== Course Relationships ==========
            modelBuilder.Entity<Course>()
                .HasMany(c => c.Lessons)
                .WithOne(l => l.Course)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Course>()
                .HasMany(c => c.Enrollments)
                .WithOne(e => e.Course)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== Lesson Relationships ==========
            modelBuilder.Entity<Lesson>()
                .HasMany(l => l.LessonProgresses)
                .WithOne(lp => lp.Lesson)
                .HasForeignKey(lp => lp.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== LessonNote configuration ==========
            modelBuilder.Entity<LessonNote>()
                .HasIndex(n => new { n.LessonId, n.StudentId })
                .IsUnique();

            modelBuilder.Entity<LessonNote>()
                .Property(n => n.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<LessonNote>()
                .Property(n => n.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<LessonNote>()
                .HasOne(n => n.Student)
                .WithMany()
                .HasForeignKey(n => n.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LessonNote>()
                .HasOne(n => n.Lesson)
                .WithMany()
                .HasForeignKey(n => n.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== Enrollment Relationships ==========
            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => new { e.StudentId, e.CourseId })
                .IsUnique();

            // ========== LessonProgress Relationships ==========
            modelBuilder.Entity<LessonProgress>()
                .HasIndex(lp => new { lp.StudentId, lp.LessonId })
                .IsUnique();

            // ========== Notification Relationships ==========
            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.UserId);

            // ========== Configurations ==========
            modelBuilder.Entity<Course>()
                .Property(c => c.Title)
                .IsRequired()
                .HasMaxLength(150);

            modelBuilder.Entity<Course>()
                .Property(c => c.Level)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Subject>()
                .Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Lesson>()
                .Property(l => l.Title)
                .IsRequired()
                .HasMaxLength(150);

            modelBuilder.Entity<Comment>()
                .Property(c => c.Content)
                .IsRequired()
                .HasMaxLength(2000);

            modelBuilder.Entity<CommentReaction>()
                .Property(cr => cr.ReactionType)
                .IsRequired()
                .HasMaxLength(20);

            modelBuilder.Entity<CommentReply>()
                .Property(cr => cr.Content)
                .IsRequired()
                .HasMaxLength(500);

            modelBuilder.Entity<CommentReport>()
                .Property(cr => cr.Reason)
                .HasMaxLength(500);

            modelBuilder.Entity<Notification>()
                .Property(n => n.Type)
                .HasMaxLength(50);

            modelBuilder.Entity<Notification>()
                .Property(n => n.Title)
                .HasMaxLength(200);

            modelBuilder.Entity<Notification>()
                .Property(n => n.Message)
                .HasMaxLength(1000);

            modelBuilder.Entity<ChatMessage>()
                .Property(cm => cm.Content)
                .HasMaxLength(2000);

            // ========== Default Values ==========
            modelBuilder.Entity<Lesson>()
                .Property(l => l.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Enrollment>()
                .Property(e => e.EnrolledAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Comment>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<LessonProgress>()
                .Property(lp => lp.StartedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<LessonProgress>()
                .Property(lp => lp.IsCompleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<CommentReaction>()
                .Property(cr => cr.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<CommentReply>()
                .Property(cr => cr.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<CommentReport>()
                .Property(cr => cr.ReportedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Notification>()
                .Property(n => n.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Chat>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<ChatMessage>()
                .Property(cm => cm.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }

        // ========== Helper Methods ==========
        public async Task<int> GetStudentProgress(string studentId, int courseId)
        {
            var totalLessons = await Lessons
                .CountAsync(l => l.CourseId == courseId);

            if (totalLessons == 0) return 0;

            var completedLessons = await LessonProgresses
                .CountAsync(lp => lp.StudentId == studentId &&
                                lp.Lesson.CourseId == courseId &&
                                lp.IsCompleted);

            return (int)Math.Round((double)completedLessons / totalLessons * 100);
        }

        public async Task<List<Course>> GetRecommendedCourses(string studentId, int limit = 6)
        {
            var enrolledCourseIds = await Enrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            return await Courses
                .Include(c => c.Subject)
                .Include(c => c.Lessons)
                .Where(c => !enrolledCourseIds.Contains(c.Id) && c.IsPublished)
                .OrderByDescending(c => c.Enrollments.Count)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetCourseProgress(string studentId)
        {
            var enrolledCourses = await Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Lessons)
                .Where(e => e.StudentId == studentId)
                .Select(e => new
                {
                    CourseId = e.CourseId,
                    TotalLessons = e.Course.Lessons.Count,
                    CompletedLessons = LessonProgresses
                        .Count(lp => lp.StudentId == studentId &&
                                    lp.Lesson.CourseId == e.CourseId &&
                                    lp.IsCompleted)
                })
                .ToListAsync();

            return enrolledCourses.ToDictionary(
                ec => ec.CourseId,
                ec => ec.TotalLessons > 0 ?
                    (int)Math.Round((double)ec.CompletedLessons / ec.TotalLessons * 100) : 0
            );
        }

        public async Task<List<Comment>> GetRecentComments(string userId, int limit = 10)
        {
            return await Comments
                .Include(c => c.User)
                .Include(c => c.Lesson)
                .ThenInclude(l => l.Course)
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> GetUnreadNotificationsCount(string userId)
        {
            return await Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<List<Notification>> GetNotifications(string userId, int limit = 20)
        {
            return await Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task MarkNotificationsAsRead(string userId)
        {
            var unreadNotifications = await Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await SaveChangesAsync();
        }

        public async Task<int> GetCommentDepth(int commentId)
        {
            var depth = 0;
            var currentCommentId = commentId;

            while (true)
            {
                var parentId = await Comments
                    .Where(c => c.Id == currentCommentId)
                    .Select(c => c.ParentCommentId)
                    .FirstOrDefaultAsync();

                if (parentId == null)
                    break;

                depth++;
                currentCommentId = parentId.Value;

                if (depth >= 10) // Safety limit
                    break;
            }

            return depth;
        }

        public async Task<List<Chat>> GetUserChats(string userId)
        {
            return await Chats
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();
        }
        // In AppDbContext.cs - Add these helper methods

        public async Task<int> GetStudentTotalTimeSpent(string studentId)
        {
            return await LessonProgresses
                .Where(lp => lp.StudentId == studentId && lp.IsCompleted)
                .SumAsync(lp => lp.TimeSpentMinutes ?? 0);
        }

        public async Task<Dictionary<string, int>> GetStudentActivityHeatmap(string studentId)
        {
            var last30Days = DateTime.UtcNow.AddDays(-30);

            var activities = await LessonProgresses
                .Where(lp => lp.StudentId == studentId &&
                            lp.CompletedAt >= last30Days)
                .GroupBy(lp => lp.CompletedAt.Value.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Count = g.Count()
                })
                .ToDictionaryAsync(a => a.Date, a => a.Count);

            return activities;
        }

        public async Task<List<Course>> GetStudentActiveCourses(string studentId, int limit = 5)
        {
            return await Enrollments
                .Include(e => e.Course)
                    .ThenInclude(c => c.Subject)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Lessons)
                .Where(e => e.StudentId == studentId)
                .OrderByDescending(e => e.EnrolledAt)
                .Select(e => e.Course)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<object> GetStudentLeaderboard(string studentId)
        {
            var studentProgress = await LessonProgresses
                .Where(lp => lp.StudentId == studentId && lp.IsCompleted)
                .CountAsync();

            var totalStudents = await Users.CountAsync(u => !u.IsTeacher);

            var betterThan = await Users
                .Where(u => !u.IsTeacher)
                .Select(u => new
                {
                    u.Id,
                    ProgressCount = LessonProgresses.Count(lp => lp.StudentId == u.Id && lp.IsCompleted)
                })
                .Where(x => x.ProgressCount < studentProgress)
                .CountAsync();

            var percentage = totalStudents > 0 ?
                (int)Math.Round((double)betterThan / totalStudents * 100) : 0;

            return new
            {
                totalStudents,
                studentProgress,
                betterThan,
                percentage,
                rank = $"{betterThan + 1}/{totalStudents}"
            };
        }
        public async Task<int> GetUnreadMessagesCount(string userId)
        {
            return await ChatMessages
                .CountAsync(m => m.Chat.User1Id == userId || m.Chat.User2Id == userId &&
                                m.SenderId != userId && !m.IsRead);
        }

        public async Task MarkChatMessagesAsRead(int chatId, string userId)
        {
            var unreadMessages = await ChatMessages
                .Where(m => m.ChatId == chatId &&
                           m.SenderId != userId &&
                           !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            await SaveChangesAsync();
        }
    }