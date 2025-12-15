// في Hubs/ChatHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Web_Lessons.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();
        private static readonly ConcurrentDictionary<string, string> _userTyping = new();

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections[userId] = Context.ConnectionId;

                // Notify user's contacts that they're online
                await Clients.All.SendAsync("UserOnline", userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections.TryRemove(userId, out _);
                _userTyping.TryRemove(userId, out _);

                // Notify user's contacts that they're offline
                await Clients.All.SendAsync("UserOffline", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinChat(int chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{chatId}");
        }

        public async Task LeaveChat(int chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat-{chatId}");
        }

        public async Task Typing(int chatId, string userId)
        {
            _userTyping[userId] = chatId.ToString();
            await Clients.Group($"chat-{chatId}").SendAsync("UserTyping", new
            {
                ChatId = chatId,
                UserId = userId,
                IsTyping = true
            });
        }

        public async Task StopTyping(int chatId, string userId)
        {
            _userTyping.TryRemove(userId, out _);
            await Clients.Group($"chat-{chatId}").SendAsync("UserTyping", new
            {
                ChatId = chatId,
                UserId = userId,
                IsTyping = false
            });
        }

        public async Task SendMessage(int chatId, string message)
        {
            var userId = Context.UserIdentifier;
            await Clients.Group($"chat-{chatId}").SendAsync("ReceiveMessage", new
            {
                ChatId = chatId,
                Message = message,
                SenderId = userId,
                Timestamp = DateTime.UtcNow
            });
        }

        public static bool IsUserOnline(string userId)
        {
            return _userConnections.ContainsKey(userId);
        }

        public static bool IsUserTyping(string userId)
        {
            return _userTyping.ContainsKey(userId);
        }

        public static string GetUserConnectionId(string userId)
        {
            _userConnections.TryGetValue(userId, out var connectionId);
            return connectionId;
        }
    }
}