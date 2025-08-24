using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WeStay.MessagingService.Services.Interfaces;
using System.Security.Claims;

namespace WeStay.MessagingService.Hubs
{
    [Authorize]
    public class MessageHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly IConversationService _conversationService;
        private readonly ILogger<MessageHub> _logger;

        public MessageHub(
            IMessageService messageService,
            IConversationService conversationService,
            ILogger<MessageHub> logger)
        {
            _messageService = messageService;
            _conversationService = conversationService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} connected to MessageHub with connection {ConnectionId}", userId, Context.ConnectionId);

            // Add user to their personal group for direct notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

            // Also add to conversations the user is part of
            var conversations = await _conversationService.GetUserConversationsAsync(userId);
            foreach (var conversation in conversations)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation-{conversation.Id}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} disconnected from MessageHub", userId);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinConversation(int conversationId)
        {
            var userId = GetUserId();

            // Verify user is part of the conversation
            var isParticipant = await _conversationService.IsUserParticipantAsync(conversationId, userId);
            if (!isParticipant)
            {
                _logger.LogWarning("User {UserId} attempted to join conversation {ConversationId} without access", userId, conversationId);
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
            _logger.LogInformation("User {UserId} joined conversation {ConversationId}", userId, conversationId);
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
            _logger.LogInformation("User {UserId} left conversation {ConversationId}", GetUserId(), conversationId);
        }

        public async Task SendMessage(int conversationId, string content, string messageType = "text")
        {
            var userId = GetUserId();

            try
            {
                // Create and save the message
                var message = await _messageService.CreateMessageAsync(conversationId, userId, content, messageType);

                // Notify all participants in the conversation
                await Clients.Group($"conversation-{conversationId}")
                    .SendAsync("ReceiveMessage", message);

                _logger.LogInformation("User {UserId} sent message to conversation {ConversationId}", userId, conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message from user {UserId} to conversation {ConversationId}", userId, conversationId);
                await Clients.Caller.SendAsync("Error", "Failed to send message");
            }
        }

        public async Task Typing(int conversationId, bool isTyping)
        {
            var userId = GetUserId();
            await Clients.Group($"conversation-{conversationId}")
                .SendAsync("UserTyping", userId, isTyping);
        }

        public async Task MarkAsRead(int messageId)
        {
            var userId = GetUserId();

            try
            {
                await _messageService.MarkMessageAsReadAsync(messageId, userId);

                // Notify other participants that the message was read
                var message = await _messageService.GetMessageByIdAsync(messageId);
                if (message != null)
                {
                    await Clients.Group($"conversation-{message.ConversationId}")
                        .SendAsync("MessageRead", messageId, userId, DateTime.UtcNow);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message {MessageId} as read by user {UserId}", messageId, userId);
            }
        }

        private int GetUserId()
        {
            return int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }
    }
}