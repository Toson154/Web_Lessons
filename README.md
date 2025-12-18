Web Lessons - Online Learning Platform
Project Overview
Web Lessons is a comprehensive online learning management system (LMS) built with ASP.NET Core MVC. The platform enables teachers to create and manage educational content while providing students with interactive learning experiences, including video lessons, chat systems, progress tracking, and collaborative features.

Developer: Toson Abdeluahab
Email: tosonaligadallah@gmail.com
Project Type: Full-Stack ASP.NET Core Web Application
Architecture: MVC Pattern with Entity Framework Core
Database: SQL Server (Code-First Approach)

Features & Capabilities
🔐 Authentication & Authorization System
Role-based access control (Student, Teacher, Admin)

Registration with role selection (Student/Teacher)

Secure login/logout with remember me functionality

Password management (change, reset)

Profile management with image uploads

Claim-based authorization for fine-grained access control

👨‍🎓 Student Features
Interactive Dashboard with learning statistics and progress tracking

Course Enrollment system with browsing and filtering capabilities

Video Lessons with integrated notes system

Progress Tracking with completion status and time spent

Comments & Reactions system for lesson discussions

Chat System for communication with teachers

Notifications for course updates and messages

Learning Analytics with weekly stats and achievement tracking

👨‍🏫 Teacher Features
Content Management System for subjects, courses, and lessons

Video Upload with chunked upload support (up to 2GB)

Student Management with enrollment tracking

Interactive Lesson Editor with PDF attachments

Real-time Chat with students

Comment Moderation system for lesson discussions

Analytics Dashboard with student progress monitoring

👨‍💼 Admin Features
User Management with role assignment and status control

System-wide Statistics and analytics

Content Oversight across all teachers and students

System Settings configuration

Bulk Operations for user management

💬 Real-time Communication
SignalR-powered Chat System with typing indicators

Online/Offline Status tracking

Notification System with real-time updates

Group Chat capabilities for course discussions

📊 Advanced Features
Chunked Video Upload for large files

PDF Integration for supplementary materials

Comments with Reactions (like, love, wow, sad, angry, haha)

Mentions System (@username notifications)

Notes System for personal annotations

Progress Analytics with charts and statistics

Search & Filter across courses and content

Responsive Design for mobile and desktop

Technology Stack
Backend
ASP.NET Core 6.0+ MVC Framework

Entity Framework Core with SQL Server

Identity Framework for authentication

SignalR for real-time features

AutoMapper for object mapping (if used)

Logging with ILogger interface

Frontend
Razor Views with HTML5, CSS3, JavaScript

Bootstrap 5 for responsive design

jQuery & AJAX for asynchronous operations

Font Awesome for icons

Custom CSS/JS for enhanced UI/UX

Database
SQL Server with Entity Framework migrations

Code-First Approach with automatic migrations

Relationships: One-to-Many, Many-to-Many configurations

Indexes for performance optimization

File Storage
Local File System for uploads (videos, PDFs, images)

Organized Structure: /uploads/videos/, /uploads/pdfs/, /uploads/profile-images/

Chunked Uploads for large video files

Project Structure
text
Web_Lessons/
├── Controllers/
│   ├── AccountController.cs           # Authentication & user management
│   ├── AdminController.cs             # Admin functionalities
│   ├── ChatController.cs              # Chat system management
│   ├── CommentsController.cs          # API for comments system
│   ├── HomeController.cs              # Public pages
│   ├── NotificationController.cs      # API for notifications
│   ├── StudentController.cs           # Student functionalities
│   ├── TeacherController.cs           # Teacher functionalities
│   └── UploadController.cs            # File upload handling
├── Models/
│   ├── ApplicationUser.cs             # Extended IdentityUser
│   ├── Subject.cs                     # Academic subjects
│   ├── Course.cs                      # Courses model
│   ├── Lesson.cs                      # Lessons model
│   ├── Comment.cs                     # Comments system
│   ├── Chat.cs                        # Chat system
│   ├── Notification.cs                # Notifications
│   └── AppDbContext.cs                # Database context
├── Views/
│   ├── Shared/                        # Layouts & partials
│   ├── Account/                       # Login, register, profile
│   ├── Admin/                         # Admin dashboard & management
│   ├── Student/                       # Student interface
│   ├── Teacher/                       # Teacher interface
│   ├── Chat/                          # Chat interface
│   └── Home/                          # Public pages
├── ViewModels/
│   └── [Various DTOs]                 # Data transfer objects
├── Hubs/
│   └── ChatHub.cs                     # SignalR chat hub
├── Migrations/
│   └── [EF Core migrations]           # Database migrations
├── wwwroot/
│   ├── css/                           # Stylesheets
│   ├── js/                            # JavaScript files
│   ├── images/                        # Static images
│   └── uploads/                       # User uploads
└── Program.cs                         # Application entry point
Database Schema
Core Entities
ApplicationUser (extends IdentityUser)

FullName, IsTeacher, ProfileImageUrl, Bio

CreatedOn, LastLogin, User preferences

Subject

Name, Description, ImageUrl, TeacherId

One teacher can have multiple subjects

Course

Title, Description, Level, ThumbnailUrl

IsPublished, SubjectId (FK to Subject)

Lesson

Title, Description, VideoUrl, PdfUrl

Order, DurationMinutes, CourseId (FK to Course)

Enrollment

StudentId (FK to ApplicationUser), CourseId (FK to Course)

EnrolledAt, CompletedAt

Comment

Content, UserId, LessonId, ParentCommentId

Reactions, Replies, Mentions system

Chat & ChatMessage

User1Id, User2Id, Messages with read status

Real-time communication

Notification

UserId, Type, Title, Message

RelatedId, RelatedType, Read status

LessonProgress

StudentId, LessonId, IsCompleted

Time tracking and completion data

LessonNote

StudentId, LessonId, Content

Personal notes for lessons

Key Functionalities Detailed
1. Authentication Flow
text
1. User Registration → Role Selection (Student/Teacher)
2. Email Confirmation → Automatic Login
3. Role-based Redirect → Student/Teacher Dashboard
4. Profile Completion → Upload image, set bio
2. Student Learning Flow
text
1. Browse Courses → Filter by subject, level
2. Enroll in Course → Single click enrollment
3. Access Lessons → Sequential or selective access
4. Watch Videos → With notes and PDF support
5. Track Progress → Automatic completion tracking
6. Interact → Comments, reactions, chat with teacher
3. Teacher Content Management
text
1. Create Subject → Define academic area
2. Create Course → Add title, description, level
3. Upload Lessons → Video upload with chunking
4. Add Materials → PDF attachments
5. Monitor Students → View progress, answer questions
6. Communicate → Chat with enrolled students
4. Admin Management
text
1. User Management → Create, edit, delete users
2. Role Assignment → Assign Teacher/Student/Admin roles
3. Content Oversight → View all courses and lessons
4. System Analytics → Platform-wide statistics
5. Settings → Configure platform parameters
API Endpoints
Authentication API
POST /Account/SignUp - User registration

POST /Account/Login - User authentication

POST /Account/Logout - User logout

GET /Account/Profile - Get user profile

POST /Account/Profile - Update profile

Comments API (/api/comments)
GET /lesson/{lessonId} - Get lesson comments

POST / - Create new comment

PUT /{id} - Edit comment

DELETE /{id} - Delete comment

POST /{commentId}/react - Add reaction

GET /{commentId}/reactions - Get reactions

Notifications API (/api/notification)
GET / - Get user notifications

GET /count - Get unread count

PUT /{id}/read - Mark as read

PUT /read-all - Mark all as read

DELETE /{id} - Delete notification

Upload API (/api/upload)
POST /chunk - Upload file chunk

POST /complete - Complete file upload

GET /check - Check upload status

POST /cancel - Cancel upload

Chat SignalR Hub (/chathub)
JoinChat - Join chat room

LeaveChat - Leave chat room

Typing - Send typing indicator

StopTyping - Clear typing indicator

SendMessage - Send chat message

Configuration & Setup
Prerequisites
.NET 6.0 SDK or later

SQL Server (LocalDB or full instance)

Visual Studio 2022 or VS Code

IIS Express (for development)

Installation Steps
Clone the repository

bash
git clone [repository-url]
cd Web_Lessons
Configure Database Connection

json
// appsettings.json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WebLessonsDB;Trusted_Connection=True;MultipleActiveResultSets=true"
}
Apply Database Migrations

bash
dotnet ef database update
# Or in Package Manager Console:
# Update-Database
Configure File Upload Paths
Ensure wwwroot/uploads/ directory exists with subdirectories:

videos/ - For video files

pdfs/ - For PDF materials

profile-images/ - For user avatars

course-images/ - For course thumbnails

subject-images/ - For subject images

Set Up Roles

csharp
// Seed method or initial setup
await roleManager.CreateAsync(new IdentityRole("Admin"));
await roleManager.CreateAsync(new IdentityRole("Teacher"));
await roleManager.CreateAsync(new IdentityRole("Student"));
Configure SignalR

csharp
// Program.cs
app.MapHub<ChatHub>("/chathub");
Run the Application

bash
dotnet run
# Or
dotnet watch run
Environment Variables
bash
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=[your-connection-string]
AllowedHosts=*
FileUpload__MaxSize=2147483648 # 2GB in bytes
FileUpload__AllowedExtensions=.mp4,.mov,.avi,.mkv,.webm,.wmv,.pdf,.jpg,.jpeg,.png
Security Considerations
Authentication & Authorization
Password Policies: ASP.NET Identity default policies

Role-based Access: Strict controller-level authorization

Anti-forgery Tokens: All POST requests validated

Secure Cookies: HTTP-only, secure cookie settings

Data Protection
SQL Injection Prevention: Entity Framework parameterized queries

XSS Protection: Input validation and output encoding

File Upload Security: Extension validation, size limits

Session Management: Secure session configuration

File Upload Security
Size Limits: 2GB max for videos, 50MB for PDFs

Extension Validation: Whitelist of allowed extensions

Content Type Verification: MIME type checking

Virus Scanning: Recommended for production

Performance Optimizations
Database Optimization
Indexed Fields: Frequently queried columns

Eager Loading: Include related data efficiently

Pagination: Large datasets split into pages

Caching Strategy: Implemented for static data

Frontend Optimization
Lazy Loading: Images and videos loaded on demand

AJAX Calls: Partial page updates without full reload

Bundling & Minification: CSS and JavaScript files

CDN Usage: Bootstrap and Font Awesome from CDN

File Handling
Chunked Uploads: Large files split into chunks

Background Processing: File processing in background tasks

Storage Optimization: Proper file organization

Testing Strategy
Unit Testing
Controller Tests: Action methods and routing

Service Tests: Business logic validation

Model Tests: Data validation and constraints

Integration Testing
Database Tests: EF Core operations

API Tests: Endpoint functionality

Authentication Tests: Login/registration flows

UI Testing
Page Load Tests: Response time optimization

Form Validation: Client and server-side validation

Cross-browser Testing: Compatibility checks

Deployment Guide
Development Deployment
Local IIS

bash
dotnet publish -c Release
# Deploy to IIS with proper permissions
Docker Container

dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0
COPY bin/Release/net6.0/publish/ App/
WORKDIR /App
ENTRYPOINT ["dotnet", "Web_Lessons.dll"]
Production Deployment
Azure App Service

Configure connection strings

Set up Azure Blob Storage for uploads

Configure custom domain and SSL

AWS Elastic Beanstalk

Configure RDS for database

Use S3 for file storage

Set up CloudFront for CDN

Linux Server (Nginx)

bash
# Install .NET Runtime
sudo apt-get install dotnet-runtime-6.0

# Configure Nginx as reverse proxy
# Set up systemd service
# Configure firewall and SSL
Production Checklist
Update connection strings for production database

Configure email service for notifications

Set up blob storage for file uploads

Enable HTTPS with valid SSL certificate

Configure logging and monitoring

Set up backup strategy for database

Configure CDN for static files

Implement rate limiting

Set up error tracking (Application Insights/Sentry)

Maintenance & Monitoring
Regular Maintenance Tasks
Database Maintenance

Regular backups

Index optimization

Clean up old data

File Storage Cleanup

Remove orphaned files

Archive old content

Monitor storage usage

Security Updates

Update dependencies

Apply security patches

Review access logs

Monitoring Metrics
Application Performance: Response times, error rates

Database Performance: Query times, connection counts

User Engagement: Active users, course completions

System Resources: CPU, memory, disk usage

Logging Strategy
Application Logs: Errors, warnings, information

Audit Logs: User actions, administrative changes

Performance Logs: Request times, database queries

Security Logs: Authentication attempts, file uploads

Troubleshooting Guide
Common Issues & Solutions
Database Connection Issues

bash
# Check connection string
# Verify SQL Server is running
# Check firewall settings
File Upload Failures

bash
# Check folder permissions
# Verify file size limits
# Check allowed extensions
SignalR Connection Problems

javascript
// Check WebSocket support
// Verify CORS configuration
// Check firewall/network settings
Performance Issues

sql
-- Check database indexes
-- Review query execution plans
-- Monitor server resources
Debug Mode
Enable detailed error pages in development:

json
// appsettings.Development.json
"DetailedErrors": true,
"Logging": {
  "LogLevel": {
    "Default": "Debug",
    "Microsoft": "Debug",
    "Microsoft.Hosting.Lifetime": "Information"
  }
}
Future Enhancements
Planned Features
Mobile Application - React Native or Flutter

Video Streaming - Adaptive bitrate streaming

Live Classes - Real-time video conferencing

Gamification - Badges, points, leaderboards

AI Recommendations - Personalized course suggestions

Multi-language Support - Internationalization

Offline Mode - Download lessons for offline viewing

Certification System - Printable certificates

Payment Integration - Paid courses and subscriptions

Analytics Dashboard - Advanced reporting

Technical Improvements
Microservices Architecture - Split monolith

GraphQL API - Flexible data queries

Redis Caching - Improved performance

Docker Compose - Easy local development

CI/CD Pipeline - Automated testing and deployment

Load Testing - Performance benchmarking

Accessibility Improvements - WCAG compliance

PWA Features - Installable web app

Contributing Guidelines
Code Standards
Follow C# coding conventions

Use meaningful variable names

Add XML documentation comments

Write unit tests for new features

Update README for significant changes

Pull Request Process
Fork the repository

Create a feature branch

Make changes with tests

Update documentation

Submit pull request with description

Commit Message Format
text
[Type]: Brief description

Detailed explanation if needed

Fixes #IssueNumber
Types: feat, fix, docs, style, refactor, test, chore

License & Copyright
License
This project is proprietary software. All rights reserved.

Copyright Notice
© 2024 Toson Abdeluahab. All rights reserved.

Contact Information
Developer: Toson Abdeluahab
Email: tosonaligadallah@gmail.com
Project: Web Lessons - Online Learning Platform

Acknowledgments
Technologies Used
ASP.NET Core - Microsoft

Entity Framework Core - Microsoft

Bootstrap - Twitter

SignalR - Microsoft

Font Awesome - Fonticons, Inc.

jQuery - JS Foundation

Inspiration
Modern LMS platforms like Udemy, Coursera

Best practices in web development

Educational technology trends

Quick Reference
Default User Roles
Admin - Full system access

Teacher - Content creation and management

Student - Course enrollment and learning

Default Ports
Development: https://localhost:5001 or http://localhost:5000

SignalR: Same as application (negotiated transport)

Important URLs
/ - Home page

/Account/Login - Login page

/Student/Dashboard - Student dashboard

/Teacher/Dashboard - Teacher dashboard

/Admin/Dashboard - Admin dashboard

/Chat - Chat interface

Sample Data
After first run, create:

Admin user (manual or seed)

Test teacher account

Sample courses for demonstration

Last Updated: December 2024
Version: 1.0.0
Status: Production Ready

For questions, support, or collaboration opportunities, please contact Toson Abdeluahab at tosonaligadallah@gmail.com
