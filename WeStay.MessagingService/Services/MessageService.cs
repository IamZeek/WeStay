using WeStay.MessagingService.DTOs;
using WeStay.MessagingService.Models;
using WeStay.MessagingService.Repositories.Interfaces;
using WeStay.MessagingService.Services.Interfaces;

namespace WeStay.MessagingService.Services
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly ILogger<MessageService> _logger;

        public MessageService(
            IMessageRepository messageRepository,
            IConversationRepository conversationRepository,
            ILogger<MessageService> logger)
        {
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
            _logger = logger;
        }

        public async Task<Message> GetMessageByIdAsync(int id)
        {
            return await _messageRepository.GetMessageByIdAsync(id);
        }

        public async Task<IEnumerable<MessageResponse>> GetConversationMessagesAsync(int conversationId, int userId, int page = 1, int pageSize = 50)
        {
            var messages = await _messageRepository.GetConversationMessagesAsync(conversationId, page, pageSize);
            return messages.Select(m => MapToMessageResponse(m, userId)).Reverse(); // Return in chronological order
        }

        public async Task<Message> CreateMessageAsync(int conversationId, int senderId, string content, string messageType = "text")
        {
            // Verify user is participant in the conversation
            var isParticipant = await _conversationRepository.IsUserParticipantAsync(conversationId, senderId);
            if (!isParticipant)
            {
                throw new UnauthorizedAccessException("User is not a participant in this conversation");
            }

            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content,
                MessageType = messageType
            };

            return await _messageRepository.CreateMessageAsync(message);
        }

        public async Task<Message> UpdateMessageAsync(int messageId, string content)
        {
            var message = await _messageRepository.GetMessageByIdAsync(messageId);
            if (message == null)
            {
                throw new KeyNotFoundException("Message not found");
            }

            message.Content = content;
            return await _messageRepository.UpdateMessageAsync(message);
        }

        public async Task<bool> DeleteMessageAsync(int messageId)
        {
            return await _messageRepository.DeleteMessageAsync(messageId);
        }

        public async Task<bool> MarkMessageAsReadAsync(int messageId, int userId)
        {
            return await _messageRepository.MarkMessageAsReadAsync(messageId, userId);
        }

        public async Task<bool> MarkAllAsReadAsync(int conversationId, int userId)
        {
            return await _messageRepository.MarkAllAsReadAsync(conversationId, userId);
        }

        public async Task<int> GetMessageCountAsync(int conversationId)
        {
            return await _messageRepository.GetMessageCountAsync(conversationId);
        }

        private MessageResponse MapToMessageResponse(Message message, int currentUserId)
        {
            return new MessageResponse
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                SenderName = $"User{message.SenderId}", // Would come from user service
                Content = message.Content,
                MessageType = message.MessageType,
                FileUrl = message.FileUrl,
                FileName = message.FileName,
                FileSize = message.FileSize,
                IsEdited = message.IsEdited,
                EditedAt = message.EditedAt,
                CreatedAt = message.CreatedAt,
                IsRead = message.MessageReads.Any(mr => mr.UserId == currentUserId),
                ReadAt = message.MessageReads.FirstOrDefault(mr => mr.UserId == currentUserId)?.ReadAt
            };
        }
    }
}