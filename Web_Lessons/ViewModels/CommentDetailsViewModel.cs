using System;
using System.Collections.Generic;
using Web_Lessons.Models;

namespace Web_Lessons.ViewModels
{
    public class CommentDetailsViewModel
    {
        public Comment Comment { get; set; }
        public List<CommentReply> Replies { get; set; } = new List<CommentReply>();
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanReport { get; set; }
        public string CurrentUserId { get; set; }
        public int LessonId { get; set; }
        public string LessonTitle { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
    }
}