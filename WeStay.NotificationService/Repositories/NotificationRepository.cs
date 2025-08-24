using Microsoft.EntityFrameworkCore;
using WeStay.NotificationService.Data;
using WeStay.NotificationService.Models;
using WeStay.NotificationService.Repositories.Interfaces;

namespace WeStay.NotificationService.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly NotificationDbContext _context;
        private readonly ILogger<NotificationRepository> _logger;

        public NotificationRepository(NotificationDbContext context, ILogger<NotificationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Notification> GetNotificationByIdAsync(int id)
        {
            return await _context.Notifications
                .Include(n => n.Type)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(int userId, int page = 1, int pageSize = 20)
        {
            return await _context.Notifications
                .Include(n => n.Type)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Include(n => n.Type)
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetPendingNotificationsAsync(int limit = 100)
        {
            return await _context.Notifications
                .Include(n => n.Type)
                .Where(n => !n.IsSent && n.RetryCount < 3)
                .OrderBy(n => n.Priority)
                .ThenBy(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<Notification> CreateNotificationAsync(Notification notification)
        {
            notification.CreatedAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created notification {NotificationId} for user {UserId}", notification.Id, notification.UserId);

            return notification;
        }

        public async Task<Notification> UpdateNotificationAsync(Notification notification)
        {
            notification.UpdatedAt = DateTime.UtcNow;
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            var notification = await GetNotificationByIdAsync(notificationId);
            if (notification == null) return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAsSentAsync(int notificationId, string externalId = null)
        {
            var notification = await GetNotificationByIdAsync(notificationId);
            if (notification == null) return false;

            notification.IsSent = true;
            notification.SentAt = DateTime.UtcNow;
            notification.ExternalId = externalId;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RecordErrorAsync(int notificationId, string errorMessage)
        {
            var notification = await GetNotificationByIdAsync(notificationId);
            if (notification == null) return false;

            notification.ErrorMessage = errorMessage;
            notification.RetryCount++;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }
    }
}