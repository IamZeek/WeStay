using WeStay.NotificationService.Models;

namespace WeStay.NotificationService.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string textContent = null);
        Task<bool> SendTemplatedEmailAsync(string toEmail, string templateName, Dictionary<string, string> variables);
        Task<bool> SendBookingConfirmationEmailAsync(string toEmail, string userName, string bookingCode, DateTime checkInDate, DateTime checkOutDate);
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink, string userName = null);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);
    }
}