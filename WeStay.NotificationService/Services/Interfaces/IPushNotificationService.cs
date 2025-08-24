using WeStay.NotificationService.Models;

namespace WeStay.NotificationService.Services.Interfaces
{
    public interface IPushNotificationService
    {
        Task<bool> SendPushAsync(int userId, string title, string message, Dictionary<string, string> data = null);
        Task<bool> SubscribeToTopicAsync(int userId, string topic);
        Task<bool> UnsubscribeFromTopicAsync(int userId, string topic);
        Task<bool> SendBookingConfirmationPushAsync(int userId, string bookingCode, DateTime checkInDate);
    }
}