# 🎓 Web Lessons – Online Learning Platform

## 🚀 Quick Overview

**Web Lessons** is an online learning platform built with **ASP.NET Core MVC**. It allows teachers to create courses and lessons, while students learn using interactive and real-time features.

---

## 👥 User Roles

### 👨‍🎓 Student

* Browse and enroll in courses
* Watch video lessons
* Track learning progress
* Chat with teachers in real time

### 👨‍🏫 Teacher

* Create and manage subjects, courses, and lessons
* Upload lesson videos and materials
* Monitor student progress
* Moderate comments and discussions

### 🛡️ Admin

* Manage users (CRUD)
* Manage roles and permissions
* View system-wide statistics and analytics

---

## 🛠️ Tech Stack

**Backend**

* ASP.NET Core MVC
* Entity Framework Core
* ASP.NET Identity

**Frontend**

* Razor Views
* Bootstrap 5
* jQuery

**Database**

* SQL Server

**Real-time Features**

* SignalR (Chat & Notifications)

---

## 📁 Project Structure

```text
Controllers/                 # MVC Controllers
├── AccountController.cs     # Authentication & Profile
├── StudentController.cs     # Student features
├── TeacherController.cs     # Teacher features
├── AdminController.cs       # Admin features
├── ChatController.cs        # Chat system
└── UploadController.cs      # File uploads

Models/                      # Database entities
├── ApplicationUser.cs
├── Subject.cs
├── Course.cs
├── Lesson.cs
├── Comment.cs
├── Chat.cs
├── Notification.cs
└── AppDbContext.cs

Views/                       # Razor views
wwwroot/                     # Static files (CSS, JS, uploads)
Hubs/                        # SignalR hubs
Migrations/                  # EF Core migrations
```

---

## 🚀 Key Features

### 🎓 For Students

* Course enrollment & progress tracking
* Video lessons with notes and comments
* Real-time chat with teachers
* Notification system

### 👨‍🏫 For Teachers

* Course and lesson management
* Chunked video upload (up to **2GB**)
* Student progress monitoring
* Comment moderation

### 🛡️ For Admins

* User management (CRUD operations)
* System-wide analytics
* Role management

---

## ⚙️ Quick Setup

### 1️⃣ Clone & Configure

```bash
git clone [repo-url]
cd Web_Lessons
```

Update `appsettings.json` with your SQL Server connection string.

---

### 2️⃣ Database Setup

```bash
dotnet ef database update
```

Or using Visual Studio:

```text
Tools → NuGet Package Manager → Package Manager Console
Update-Database
```

---

### 3️⃣ Create Upload Folders

```bash
mkdir -p wwwroot/uploads/videos
mkdir -p wwwroot/uploads/pdfs
mkdir -p wwwroot/uploads/profile-images
```

---

### 4️⃣ Run Application

```bash
dotnet run
```

Access the app at:

```
https://localhost:5001
```

---

## 🔐 Default Admin Account

After applying migrations, create the admin user manually:

```sql
-- Create admin user
INSERT INTO AspNetUsers (Id, UserName, Email, FullName, IsTeacher, CreatedOn)
VALUES ('admin-id', 'admin@weblessons.com', 'admin@weblessons.com', 'Admin', 1, GETDATE());

-- Assign Admin role
INSERT INTO AspNetUserRoles (UserId, RoleId)
VALUES (
  'admin-id',
  (SELECT Id FROM AspNetRoles WHERE Name = 'Admin')
);
```

---

## 📦 API Endpoints

```http
POST   /api/upload/chunk                 # Upload video chunks
GET    /api/comments/lesson/{id}         # Get lesson comments
POST   /api/comments                    # Add comment
GET    /api/notification                # Get notifications
```

**SignalR**

```text
/chathub    # Real-time chat & notifications
```

---

## ⚠️ Important Notes

* **File Uploads**: Ensure write permissions for `wwwroot/uploads/`
* **Video Size Limit**: Up to **2GB** (chunked upload supported)
* **Security**: ASP.NET Identity + AntiForgery Tokens
* **Database**: SQL Server (LocalDB recommended for development)

---

## 🔧 Configuration Files

* `appsettings.json` – Connection strings, logging
* `Program.cs` – Services and middleware pipeline
* `launchSettings.json` – Debug profiles

---

## 🚨 Troubleshooting

* **Database errors**: Check connection string in `appsettings.json`
* **Upload failures**: Verify folder permissions for `wwwroot/uploads/`
* **SignalR issues**: Check browser console for WebSocket errors
* **Login problems**: Ensure user exists and password is correct

---

## 📞 Contact

**Developer:** Toson Abdeluahab
**Email:** [tosonaligadallah@gmail.com](mailto:tosonaligadallah@gmail.com)
