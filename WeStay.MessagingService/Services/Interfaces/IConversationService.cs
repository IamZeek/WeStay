using WeStay.MessagingService.DTOs;
using WeStay.MessagingService.Models;

namespace WeStay.MessagingService.Services.Interfaces
{
    public interface IConversationService
    {
        Task<Conversation> GetConversationByIdAsync(int id);
        Task<Conversation> GetConversationByGuidAsync(Guid guid);
        Task<IEnumerable<ConversationResponse>> GetUserConversationsAsync(int userId);
        Task<Conversation> CreateConversationAsync(CreateConversationRequest request);
        Task<bool> AddParticipantAsync(int conversationId, int userId);
        Task<bool> RemoveParticipantAsync(int conversationId, int userId);
        Task<bool> ArchiveConversationAsync(int conversationId);
        Task<bool> IsUserParticipantAsync(int conversationId, int userId);
        Task<int> GetUnreadCountAsync(int conversationId, int userId);
        Task<bool> MarkConversationAsReadAsync(int conversationId, int userId);
    }
}