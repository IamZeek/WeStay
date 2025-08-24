using WeStay.NotificationService.DTOs;
using WeStay.NotificationService.Models;

namespace WeStay.NotificationService.Services.Interfaces
{
    public interface INotificationServices
    {
        Task<Notification> SendNotificationAsync(SendNotificationRequest request);
        Task<Notification> SendTemplatedNotificationAsync(SendTemplatedNotificationRequest request);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task ProcessPendingNotificationsAsync();

    }
}