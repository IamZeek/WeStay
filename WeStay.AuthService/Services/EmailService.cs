using SendGrid.Helpers.Mail;
using SendGrid;

namespace WeStay.AuthService.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpAsync(string email, string otp)
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(_config["SendGrid:FromEmail"], _config["SendGrid:FromName"]);
            var subject = "Your OTP Code";
            var to = new EmailAddress(email);
            var plainTextContent = $"Your OTP code is: {otp}";
            var htmlContent = $"<strong>Your OTP code is: {otp}</strong>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
        }
    }
}
