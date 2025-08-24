using SendGrid;
using SendGrid.Helpers.Mail;

public interface IEmailService
{
    Task SendOtpAsync(string email, string otp);
}


