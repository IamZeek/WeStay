using WeStay.MessagingService.DTOs;
using WeStay.MessagingService.Models;

namespace WeStay.MessagingService.Services.Interfaces
{
    public interface IMessageService
    {
        Task<Message> GetMessageByIdAsync(int id);
        Task<IEnumerable<MessageResponse>> GetConversationMessagesAsync(int conversationId, int userId, int page = 1, int pageSize = 50);
        Task<Message> CreateMessageAsync(int conversationId, int senderId, string content, string messageType = "text");
        Task<Message> UpdateMessageAsync(int messageId, string content);
        Task<bool> DeleteMessageAsync(int messageId);
        Task<bool> MarkMessageAsReadAsync(int messageId, int userId);
        Task<bool> MarkAllAsReadAsync(int conversationId, int userId);
        Task<int> GetMessageCountAsync(int conversationId);
    }
}