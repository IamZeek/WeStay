using WeStay.NotificationService.Models;

namespace WeStay.NotificationService.Repositories.Interfaces
{
    public interface INotificationTypeRepository
    {
        Task<IEnumerable<NotificationType>> GetAllTypesAsync();
        Task<NotificationType> GetTypeByIdAsync(int id);
        Task<NotificationType> GetTypeByNameAsync(string name);
    }
}