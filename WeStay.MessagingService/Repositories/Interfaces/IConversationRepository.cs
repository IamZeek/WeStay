using WeStay.MessagingService.Models;

namespace WeStay.MessagingService.Repositories.Interfaces
{
    public interface IConversationRepository
    {
        Task<Conversation> GetConversationByIdAsync(int id);
        Task<Conversation> GetConversationByGuidAsync(Guid guid);
        Task<IEnumerable<Conversation>> GetUserConversationsAsync(int userId);
        Task<IEnumerable<Conversation>> GetConversationsByListingAsync(int listingId);
        Task<IEnumerable<Conversation>> GetConversationsByBookingAsync(int bookingId);
        Task<Conversation> CreateConversationAsync(Conversation conversation);
        Task<Conversation> UpdateConversationAsync(Conversation conversation);
        Task<bool> DeleteConversationAsync(int id);
        Task<bool> ArchiveConversationAsync(int id);
        Task<bool> IsUserParticipantAsync(int conversationId, int userId);
        Task<int> GetUnreadCountAsync(int conversationId, int userId);
        Task<bool> AddParticipantAsync(int conversationId, int userId);
        Task<bool> RemoveParticipantAsync(int conversationId, int userId);
        Task<bool> UpdateLastReadAsync(int conversationId, int userId);
    }
}