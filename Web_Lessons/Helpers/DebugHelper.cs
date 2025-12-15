using System;
using System.IO;

namespace Web_Lessons.Helpers
{
    public static class DebugHelper
    {
        public static void LogToFile(string message, string fileName = "debug.log")
        {
            try
            {
                var logPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
                if (!Directory.Exists(logPath))
                    Directory.CreateDirectory(logPath);

                var fullPath = Path.Combine(logPath, fileName);
                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";

                File.AppendAllText(fullPath, logMessage);
            }
            catch
            {
                // Ignore logging errors
            }
        }

        public static string GetCurrentUserInfo(HttpContext context)
        {
            try
            {
                var user = context.User;
                var userId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userName = user?.Identity?.Name;

                return $"User: {userName} (ID: {userId})";
            }
            catch
            {
                return "User: Unknown";
            }
        }
    }
}