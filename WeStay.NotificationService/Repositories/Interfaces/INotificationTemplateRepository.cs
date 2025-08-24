using WeStay.NotificationService.Models;

namespace WeStay.NotificationService.Repositories.Interfaces
{
    public interface INotificationTemplateRepository
    {
        Task<IEnumerable<NotificationTemplate>> GetAllTemplatesAsync();
        Task<NotificationTemplate> GetTemplateByIdAsync(int id);
        Task<NotificationTemplate> GetTemplateByNameAsync(string name);
        Task<NotificationTemplate> GetTemplateByTypeAndChannelAsync(string type, string channel);
        Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template);
        Task<NotificationTemplate> UpdateTemplateAsync(NotificationTemplate template);
        Task<bool> DeleteTemplateAsync(int id);
    }
}