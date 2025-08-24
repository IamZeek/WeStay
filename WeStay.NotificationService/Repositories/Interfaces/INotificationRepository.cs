using WeStay.NotificationService.Models;

namespace WeStay.NotificationService.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task<Notification> GetNotificationByIdAsync(int id);
        Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(int userId, int page = 1, int pageSize = 20);
        Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(int userId);
        Task<IEnumerable<Notification>> GetPendingNotificationsAsync(int limit = 100);
        Task<Notification> CreateNotificationAsync(Notification notification);
        Task<Notification> UpdateNotificationAsync(Notification notification);
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> MarkAsSentAsync(int notificationId, string externalId = null);
        Task<bool> RecordErrorAsync(int notificationId, string errorMessage);
        Task<int> GetUnreadCountAsync(int userId);
    }
}