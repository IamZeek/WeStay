using WeStay.NotificationService.Models;

namespace WeStay.NotificationService.Repositories.Interfaces
{
    public interface IUserPreferencesRepository
    {
        Task<UserNotificationPreferences> GetPreferencesByUserIdAsync(int userId);
        Task<UserNotificationPreferences> CreatePreferencesAsync(UserNotificationPreferences preferences);
        Task<UserNotificationPreferences> UpdatePreferencesAsync(UserNotificationPreferences preferences);
        Task<bool> AreNotificationsEnabledAsync(int userId, string channel, string notificationType);
    }
}