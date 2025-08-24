using WeStay.MessagingService.Models;

namespace WeStay.MessagingService.Repositories.Interfaces
{
    public interface IMessageRepository
    {
        Task<Message> GetMessageByIdAsync(int id);
        Task<IEnumerable<Message>> GetConversationMessagesAsync(int conversationId, int page = 1, int pageSize = 50);
        Task<IEnumerable<Message>> GetUnreadMessagesAsync(int conversationId, int userId);
        Task<Message> CreateMessageAsync(Message message);
        Task<Message> UpdateMessageAsync(Message message);
        Task<bool> DeleteMessageAsync(int id);
        Task<bool> MarkMessageAsReadAsync(int messageId, int userId);
        Task<bool> MarkAllAsReadAsync(int conversationId, int userId);
        Task<int> GetMessageCountAsync(int conversationId);
    }
}