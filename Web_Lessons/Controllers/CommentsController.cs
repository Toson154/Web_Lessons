using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Web_Lessons.Models;
using Web_Lessons.ViewModels;

namespace Web_Lessons.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommentsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/comments/lesson/{lessonId}
        [HttpGet("lesson/{lessonId}")]
        public async Task<ActionResult<CommentsResponseViewModel>> GetComments(int lessonId)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                // Check if user has access to this lesson
                var hasAccess = await HasAccessToLesson(lessonId, currentUserId, user);
                if (!hasAccess)
                {
                    return Unauthorized(new { message = "You don't have access to this lesson" });
                }

                // Get all comments for this lesson
                var comments = await _context.Comments
                    .Include(c => c.User)
                    .Include(c => c.MentionedUser)
                    .Include(c => c.Replies)
                        .ThenInclude(r => r.User)
                    .Include(c => c.Replies)
                        .ThenInclude(r => r.MentionedUser)
                    .Include(c => c.Reactions)
                    .Where(c => c.LessonId == lessonId &&
                               !c.IsDeleted &&
                               c.ParentCommentId == null)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                // Get current user's reactions
                var userReactions = await _context.CommentReactions
                    .Where(cr => cr.UserId == currentUserId &&
                                comments.Select(c => c.Id).Contains(cr.CommentId))
                    .ToDictionaryAsync(cr => cr.CommentId, cr => cr.ReactionType);

                var commentViewModels = new List<CommentViewModel>();

                foreach (var comment in comments)
                {
                    var canEdit = comment.UserId == currentUserId ||
                                 await _userManager.IsInRoleAsync(user, "Teacher") ||
                                 await _userManager.IsInRoleAsync(user, "Admin");

                    var canDelete = canEdit ||
                                   (comment.Replies != null && !comment.Replies.Any());

                    var commentVm = await MapCommentToViewModel(comment, currentUserId, canEdit, canDelete);
                    commentViewModels.Add(commentVm);
                }

                var response = new CommentsResponseViewModel
                {
                    Comments = commentViewModels,
                    TotalComments = await _context.Comments
                        .CountAsync(c => c.LessonId == lessonId && !c.IsDeleted),
                    CanComment = await CanCommentOnLesson(lessonId, currentUserId, user),
                    CurrentUserId = currentUserId
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading comments", error = ex.Message });
            }
        }

        // POST: api/comments
        [HttpPost]
        public async Task<ActionResult<CommentViewModel>> CreateComment(CreateCommentViewModel model)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                // Validate lesson access
                var hasAccess = await HasAccessToLesson(model.LessonId, currentUserId, user);
                if (!hasAccess)
                {
                    return Unauthorized(new { message = "You don't have access to this lesson" });
                }

                // Check if can comment
                if (!await CanCommentOnLesson(model.LessonId, currentUserId, user))
                {
                    return BadRequest(new { message = "You cannot comment on this lesson" });
                }

                // Validate parent comment if exists
                if (model.ParentCommentId.HasValue)
                {
                    var parentComment = await _context.Comments
                        .FirstOrDefaultAsync(c => c.Id == model.ParentCommentId.Value &&
                                                 c.LessonId == model.LessonId &&
                                                 !c.IsDeleted);
                    if (parentComment == null)
                    {
                        return BadRequest(new { message = "Parent comment not found" });
                    }
                }

                // Process mentions in content
                var processedContent = await ProcessMentions(model.Content);

                var comment = new Comment
                {
                    Content = processedContent,
                    UserId = currentUserId,
                    LessonId = model.LessonId,
                    ParentCommentId = model.ParentCommentId,
                    MentionedUserId = model.MentionedUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                // Load the created comment with relations
                var createdComment = await _context.Comments
                    .Include(c => c.User)
                    .Include(c => c.MentionedUser)
                    .Include(c => c.Reactions)
                    .FirstOrDefaultAsync(c => c.Id == comment.Id);

                var commentVm = await MapCommentToViewModel(createdComment, currentUserId, true, true);

                return Ok(new
                {
                    success = true,
                    comment = commentVm,
                    message = "Comment added successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating comment", error = ex.Message });
            }
        }

        // PUT: api/comments/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> EditComment(int id, EditCommentViewModel model)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                var comment = await _context.Comments
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (comment == null)
                {
                    return NotFound(new { message = "Comment not found" });
                }

                // Check if user can edit this comment
                var canEdit = comment.UserId == currentUserId ||
                             await _userManager.IsInRoleAsync(user, "Teacher") ||
                             await _userManager.IsInRoleAsync(user, "Admin");

                if (!canEdit)
                {
                    return Unauthorized(new { message = "You cannot edit this comment" });
                }

                // Process mentions in content
                var processedContent = await ProcessMentions(model.Content);

                comment.Content = processedContent;
                comment.IsEdited = true;
                comment.UpdatedAt = DateTime.UtcNow;

                _context.Comments.Update(comment);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Comment updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating comment", error = ex.Message });
            }
        }

        // DELETE: api/comments/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteComment(int id)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                var comment = await _context.Comments
                    .Include(c => c.Replies)
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (comment == null)
                {
                    return NotFound(new { message = "Comment not found" });
                }

                // Check if user can delete this comment
                var canDelete = comment.UserId == currentUserId ||
                               await _userManager.IsInRoleAsync(user, "Teacher") ||
                               await _userManager.IsInRoleAsync(user, "Admin");

                if (!canDelete)
                {
                    return Unauthorized(new { message = "You cannot delete this comment" });
                }

                // If comment has replies, mark as deleted but keep content
                if (comment.Replies != null && comment.Replies.Any())
                {
                    comment.IsDeleted = true;
                    comment.Content = "[Comment deleted]";
                    _context.Comments.Update(comment);
                }
                else
                {
                    _context.Comments.Remove(comment);
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Comment deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting comment", error = ex.Message });
            }
        }

        // POST: api/comments/{commentId}/react
        [HttpPost("{commentId}/react")]
        public async Task<ActionResult> AddReaction(int commentId, [FromBody] string reactionType)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);

                var comment = await _context.Comments
                    .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

                if (comment == null)
                {
                    return NotFound(new { message = "Comment not found" });
                }

                // Validate reaction type
                var validReactions = new[] { "like", "love", "haha", "wow", "sad", "angry" };
                if (!validReactions.Contains(reactionType.ToLower()))
                {
                    return BadRequest(new { message = "Invalid reaction type" });
                }

                // Check if user already reacted
                var existingReaction = await _context.CommentReactions
                    .FirstOrDefaultAsync(cr => cr.CommentId == commentId && cr.UserId == currentUserId);

                if (existingReaction != null)
                {
                    // If same reaction, remove it
                    if (existingReaction.ReactionType == reactionType)
                    {
                        _context.CommentReactions.Remove(existingReaction);
                    }
                    else
                    {
                        // Update reaction
                        existingReaction.ReactionType = reactionType;
                        existingReaction.CreatedAt = DateTime.UtcNow;
                        _context.CommentReactions.Update(existingReaction);
                    }
                }
                else
                {
                    // Add new reaction
                    var reaction = new CommentReaction
                    {
                        UserId = currentUserId,
                        CommentId = commentId,
                        ReactionType = reactionType,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CommentReactions.Add(reaction);
                }

                await _context.SaveChangesAsync();

                // Get updated reaction counts
                var reactions = await _context.CommentReactions
                    .Where(cr => cr.CommentId == commentId)
                    .GroupBy(cr => cr.ReactionType)
                    .Select(g => new
                    {
                        ReactionType = g.Key,
                        Count = g.Count(),
                        IsCurrentUserReacted = g.Any(r => r.UserId == currentUserId)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    reactions = reactions,
                    message = "Reaction updated successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error adding reaction", error = ex.Message });
            }
        }

        // GET: api/comments/{commentId}/reactions
        [HttpGet("{commentId}/reactions")]
        public async Task<ActionResult> GetReactions(int commentId)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);

                var reactions = await _context.CommentReactions
                    .Include(cr => cr.User)
                    .Where(cr => cr.CommentId == commentId)
                    .GroupBy(cr => cr.ReactionType)
                    .Select(g => new
                    {
                        ReactionType = g.Key,
                        Count = g.Count(),
                        Users = g.Select(r => new
                        {
                            r.UserId,
                            r.User.FullName,
                            r.User.ProfileImageUrl
                        }).Take(5).ToList(),
                        IsCurrentUserReacted = g.Any(r => r.UserId == currentUserId)
                    })
                    .ToListAsync();

                return Ok(new { success = true, reactions = reactions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error getting reactions", error = ex.Message });
            }
        }

        #region Helper Methods

        private async Task<bool> HasAccessToLesson(int lessonId, string userId, ApplicationUser user)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .ThenInclude(c => c.Subject)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null) return false;

            // Check if user is teacher of this lesson
            if (await _userManager.IsInRoleAsync(user, "Teacher"))
            {
                return lesson.Course.Subject.TeacherId == userId;
            }

            // Check if student is enrolled in the course
            if (await _userManager.IsInRoleAsync(user, "Student"))
            {
                return await _context.Enrollments
                    .AnyAsync(e => e.StudentId == userId && e.CourseId == lesson.CourseId);
            }

            // Admin has access to everything
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return true;
            }

            return false;
        }

        private async Task<bool> CanCommentOnLesson(int lessonId, string userId, ApplicationUser user)
        {
            // Teachers and Admins can always comment
            if (await _userManager.IsInRoleAsync(user, "Teacher") ||
                await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return true;
            }

            // Students must be enrolled in the course
            if (await _userManager.IsInRoleAsync(user, "Student"))
            {
                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.Id == lessonId);

                if (lesson == null) return false;

                return await _context.Enrollments
                    .AnyAsync(e => e.StudentId == userId && e.CourseId == lesson.CourseId);
            }

            return false;
        }

        private async Task<CommentViewModel> MapCommentToViewModel(Comment comment, string currentUserId, bool canEdit, bool canDelete)
        {
            // Get reactions for this comment
            var reactions = await _context.CommentReactions
                .Where(cr => cr.CommentId == comment.Id)
                .GroupBy(cr => cr.ReactionType)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Count()
                );

            // Get current user's reaction
            var currentUserReaction = await _context.CommentReactions
                .Where(cr => cr.CommentId == comment.Id && cr.UserId == currentUserId)
                .Select(cr => cr.ReactionType)
                .FirstOrDefaultAsync();

            var commentVm = new CommentViewModel
            {
                Id = comment.Id,
                Content = comment.Content,
                UserId = comment.UserId,
                UserName = comment.User?.FullName ?? "Unknown User",
                UserProfileImage = comment.User?.ProfileImageUrl,
                IsTeacher = comment.User?.IsTeacher ?? false,
                ParentCommentId = comment.ParentCommentId,
                MentionedUserId = comment.MentionedUserId,
                MentionedUserName = comment.MentionedUser?.FullName,
                IsEdited = comment.IsEdited,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                Reactions = reactions,
                CurrentUserReaction = currentUserReaction,
                CanEdit = canEdit,
                CanDelete = canDelete
            };

            // Add replies if any
            if (comment.Replies != null && comment.Replies.Any())
            {
                foreach (var reply in comment.Replies.Where(r => !r.IsDeleted).OrderBy(r => r.CreatedAt))
                {
                    var canEditReply = reply.UserId == currentUserId || canEdit;
                    var canDeleteReply = canEditReply || !reply.Replies.Any();

                    var replyVm = await MapCommentToViewModel(reply, currentUserId, canEditReply, canDeleteReply);
                    commentVm.Replies.Add(replyVm);
                }
            }

            return commentVm;
        }

        private async Task<string> ProcessMentions(string content)
        {
            // Find mentions in the format @username or @UserId
            var mentionRegex = new Regex(@"@([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})|@([a-zA-Z0-9_]{3,})");
            var matches = mentionRegex.Matches(content);

            foreach (Match match in matches)
            {
                var mentionText = match.Value.Substring(1); // Remove @

                // Try to find user by email or username
                var user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Email == mentionText ||
                                            u.UserName == mentionText ||
                                            u.FullName.Contains(mentionText));

                if (user != null)
                {
                    // Replace with a link or formatted mention
                    content = content.Replace(match.Value,
                        $"<span class='mention' data-user-id='{user.Id}'>@{user.FullName}</span>");
                }
            }

            return content;
        }

        #endregion
    }
}