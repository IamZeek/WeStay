using Microsoft.EntityFrameworkCore;
using WeStay.NotificationService.Data;
using WeStay.NotificationService.Models;
using WeStay.NotificationService.Repositories.Interfaces;

namespace WeStay.NotificationService.Repositories
{
    public class UserPreferencesRepository : IUserPreferencesRepository
    {
        private readonly NotificationDbContext _context;
        private readonly ILogger<UserPreferencesRepository> _logger;

        public UserPreferencesRepository(NotificationDbContext context, ILogger<UserPreferencesRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserNotificationPreferences> GetPreferencesByUserIdAsync(int userId)
        {
            return await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<UserNotificationPreferences> CreatePreferencesAsync(UserNotificationPreferences preferences)
        {
            preferences.CreatedAt = DateTime.UtcNow;
            preferences.UpdatedAt = DateTime.UtcNow;

            _context.UserNotificationPreferences.Add(preferences);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created notification preferences for user {UserId}", preferences.UserId);

            return preferences;
        }

        public async Task<UserNotificationPreferences> UpdatePreferencesAsync(UserNotificationPreferences preferences)
        {
            preferences.UpdatedAt = DateTime.UtcNow;
            _context.UserNotificationPreferences.Update(preferences);
            await _context.SaveChangesAsync();

            return preferences;
        }

        public async Task<bool> AreNotificationsEnabledAsync(int userId, string channel, string notificationType)
        {
            var preferences = await GetPreferencesByUserIdAsync(userId);
            if (preferences == null) return true; // Default to enabled if no preferences set

            return channel.ToLower() switch
            {
                "email" => preferences.EmailEnabled && IsNotificationTypeEnabled(preferences, notificationType),
                "sms" => preferences.SMSEnabled && IsNotificationTypeEnabled(preferences, notificationType),
                "push" => preferences.PushEnabled && IsNotificationTypeEnabled(preferences, notificationType),
                "inapp" => true, // In-app notifications are always enabled
                _ => false
            };
        }

        private bool IsNotificationTypeEnabled(UserNotificationPreferences preferences, string notificationType)
        {
            return notificationType.ToLower() switch
            {
                "bookingconfirmation" or "bookingcancellation" or "paymentreceived" or "paymentfailed" => preferences.BookingNotifications,
                "securityalert" or "passwordreset" => preferences.SecurityNotifications,
                "welcome" or "marketing" => preferences.MarketingEmails,
                "newsletter" => preferences.Newsletter,
                _ => true
            };
        }
    }
}