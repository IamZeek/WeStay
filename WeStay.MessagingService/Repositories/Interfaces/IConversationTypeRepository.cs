using WeStay.MessagingService.Models;

namespace WeStay.MessagingService.Repositories.Interfaces
{
    public interface IConversationTypeRepository
    {
        Task<IEnumerable<ConversationType>> GetAllTypesAsync();
        Task<ConversationType> GetTypeByIdAsync(int id);
        Task<ConversationType> GetTypeByNameAsync(string name);
    }
}