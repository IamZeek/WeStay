using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;
using System.Net.Mail;
using WeStay.NotificationService.Models;
using WeStay.NotificationService.Services.Interfaces;
using WeStay.NotificationService.Repositories.Interfaces;

namespace WeStay.NotificationService.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly INotificationTemplateRepository _templateRepository;
        private readonly ITemplateService _templateService;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailService> logger,
            INotificationTemplateRepository templateRepository,
            ITemplateService templateService)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _templateRepository = templateRepository;
            _templateService = templateService;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string textContent = null)
        {
            try
            {
                if (_emailSettings.UseSendGrid && !string.IsNullOrEmpty(_emailSettings.SendGridApiKey))
                {
                    return await SendWithSendGridAsync(toEmail, subject, htmlContent, textContent);
                }
                else
                {
                    return await SendWithSmtpAsync(toEmail, subject, htmlContent, textContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
                return false;
            }
        }

        private async Task<bool> SendWithSendGridAsync(string toEmail, string subject, string htmlContent, string textContent = null)
        {
            try
            {
                var client = new SendGridClient(_emailSettings.SendGridApiKey);
                var from = new EmailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName);
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, textContent ?? StripHtml(htmlContent), htmlContent);

                var response = await client.SendEmailAsync(msg);

                if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
                {
                    _logger.LogInformation("Email sent successfully to {Email} via SendGrid", toEmail);
                    return true;
                }

                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid API error: {StatusCode} - {ResponseBody}", response.StatusCode, responseBody);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email with SendGrid to {Email}", toEmail);
                return false;
            }
        }

        private async Task<bool> SendWithSmtpAsync(string toEmail, string subject, string htmlContent, string textContent = null)
        {
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    EnableSsl = _emailSettings.UseSSL,
                    Credentials = new NetworkCredential(_emailSettings.UserName, _emailSettings.Password)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = textContent ?? StripHtml(htmlContent),
                    IsBodyHtml = !string.IsNullOrEmpty(htmlContent)
                };

                if (!string.IsNullOrEmpty(htmlContent))
                {
                    var htmlView = AlternateView.CreateAlternateViewFromString(htmlContent, null, "text/html");
                    mailMessage.AlternateViews.Add(htmlView);
                }

                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email sent successfully to {Email} via SMTP", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email with SMTP to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendTemplatedEmailAsync(string toEmail, string templateName, Dictionary<string, string> variables)
        {
            try
            {
                var template = await _templateRepository.GetTemplateByNameAsync(templateName);
                if (template == null || template.Channel != "Email")
                {
                    _logger.LogError("Email template not found: {TemplateName}", templateName);
                    return false;
                }

                var (subject, body) = await _templateService.RenderEmailTemplateAsync(templateName, variables);

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending templated email to {Email} with template {TemplateName}",
                    toEmail, templateName);
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "Welcome to WeStay!";
            var htmlContent = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: #007bff; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background: #f9f9f9; }}
                        .footer {{ padding: 20px; text-align: center; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Welcome to WeStay!</h1>
                        </div>
                        <div class='content'>
                            <h2>Hello {userName}!</h2>
                            <p>Thank you for joining WeStay. We're excited to help you find the perfect accommodation for your travels.</p>
                            <p>Start exploring our properties and book your next stay today!</p>
                            <p><a href='https://westay.com/properties' style='background: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Browse Properties</a></p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 WeStay. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task<bool> SendBookingConfirmationEmailAsync(string toEmail, string userName, string bookingCode, DateTime checkInDate, DateTime checkOutDate)
        {
            var subject = $"Booking Confirmation - {bookingCode}";
            var htmlContent = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: #28a745; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background: #f9f9f9; }}
                        .booking-details {{ background: white; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                        .footer {{ padding: 20px; text-align: center; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Booking Confirmed!</h1>
                        </div>
                        <div class='content'>
                            <h2>Hello {userName}!</h2>
                            <p>Your booking has been successfully confirmed. Here are your booking details:</p>
                            
                            <div class='booking-details'>
                                <h3>Booking Information</h3>
                                <p><strong>Booking Code:</strong> {bookingCode}</p>
                                <p><strong>Check-in:</strong> {checkInDate:MMM dd, yyyy}</p>
                                <p><strong>Check-out:</strong> {checkOutDate:MMM dd, yyyy}</p>
                                <p><strong>Duration:</strong> {(checkOutDate - checkInDate).Days} nights</p>
                            </div>

                            <p>We look forward to hosting you! If you have any questions, please contact our support team.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 WeStay. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, htmlContent);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink, string userName = null)
        {
            var subject = "Password Reset Request";
            var htmlContent = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: #dc3545; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background: #f9f9f9; }}
                        .button {{ background: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
                        .footer {{ padding: 20px; text-align: center; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Password Reset</h1>
                        </div>
                        <div class='content'>
                            <h2>Hello {(string.IsNullOrEmpty(userName) ? "there" : userName)}!</h2>
                            <p>We received a request to reset your password. Click the button below to create a new password:</p>
                            
                            <p style='text-align: center; margin: 30px 0;'>
                                <a href='{resetLink}' class='button'>Reset Password</a>
                            </p>

                            <p>If you didn't request a password reset, please ignore this email. This link will expire in 1 hour.</p>
                            
                            <p><strong>Note:</strong> For security reasons, we recommend that you don't share this link with anyone.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 WeStay. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, htmlContent);
        }

        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Simple HTML stripping - you might want to use a more robust method
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        }
    }
}