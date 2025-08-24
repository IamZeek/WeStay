using System.Net.Mail;
using WeStay.MessagingService.Models;

namespace WeStay.MessagingService.Services
{
    public interface INotificationServices
    {
        Task<bool> SendEmailAsync(EmailMessage emailMessage);
        Task<bool> SendSmsAsync(SmsMessage smsMessage);
        Task<bool> SendPushNotificationAsync(PushNotification pushNotification);
        Task<bool> SendBroadcastNotificationAsync(BroadcastMessage broadcastMessage);
    }
}