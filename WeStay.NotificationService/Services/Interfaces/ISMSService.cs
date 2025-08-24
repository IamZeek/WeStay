using WeStay.NotificationService.Models;

namespace WeStay.NotificationService.Services.Interfaces
{
    public interface ISMSService
    {
        Task<bool> SendSMSAsync(string toPhoneNumber, string message);
        Task<bool> SendTemplatedSMSAsync(string toPhoneNumber, string templateName, Dictionary<string, string> variables);
        Task<bool> SendBookingConfirmationSMSAsync(string toPhoneNumber, string bookingCode, DateTime checkInDate);
        Task<bool> SendVerificationSMSAsync(string toPhoneNumber, string verificationCode);
        Task<bool> ValidatePhoneNumberAsync(string phoneNumber);
        Task<string> FormatPhoneNumberAsync(string phoneNumber);
    }
}