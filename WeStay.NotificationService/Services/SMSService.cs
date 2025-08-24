using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using WeStay.NotificationService.Models;
using WeStay.NotificationService.Services.Interfaces;
using WeStay.NotificationService.Repositories.Interfaces;

namespace WeStay.NotificationService.Services
{
    public class SMSService : ISMSService
    {
        private readonly TwilioSettings _twilioSettings;
        private readonly ILogger<SMSService> _logger;
        private readonly INotificationTemplateRepository _templateRepository;
        private readonly ITemplateService _templateService;
        private bool _twilioInitialized = false;

        public SMSService(
            IOptions<TwilioSettings> twilioSettings,
            ILogger<SMSService> logger,
            INotificationTemplateRepository templateRepository,
            ITemplateService templateService)
        {
            _twilioSettings = twilioSettings.Value;
            _logger = logger;
            _templateRepository = templateRepository;
            _templateService = templateService;
            InitializeTwilio();
        }

        private void InitializeTwilio()
        {
            try
            {
                if (!string.IsNullOrEmpty(_twilioSettings.AccountSid) && !string.IsNullOrEmpty(_twilioSettings.AuthToken))
                {
                    TwilioClient.Init(_twilioSettings.AccountSid, _twilioSettings.AuthToken);
                    _twilioInitialized = true;
                    _logger.LogInformation("Twilio client initialized successfully");
                }
                else
                {
                    _logger.LogWarning("Twilio credentials not configured. SMS service will not work properly.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Twilio client");
            }
        }

        public async Task<bool> SendSMSAsync(string toPhoneNumber, string message)
        {
            if (!_twilioInitialized)
            {
                _logger.LogError("Twilio client not initialized. Cannot send SMS.");
                return false;
            }

            try
            {
                // Use test number in test mode
                var recipientNumber = _twilioSettings.TestMode ? _twilioSettings.TestNumber : toPhoneNumber;

                // Validate phone number format
                if (!IsValidPhoneNumber(recipientNumber))
                {
                    _logger.LogWarning("Invalid phone number format: {PhoneNumber}", recipientNumber);
                    return false;
                }

                var messageResource = await MessageResource.CreateAsync(
                    body: message,
                    from: new PhoneNumber(_twilioSettings.FromNumber),
                    to: new PhoneNumber(recipientNumber)
                );

                _logger.LogInformation("SMS sent successfully to {PhoneNumber}. Message SID: {MessageSid}",
                    recipientNumber, messageResource.Sid);

                return messageResource.Status != MessageResource.StatusEnum.Failed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", toPhoneNumber);
                return false;
            }
        }

        public async Task<bool> SendTemplatedSMSAsync(string toPhoneNumber, string templateName, Dictionary<string, string> variables)
        {
            try
            {
                // Get the template
                var template = await _templateRepository.GetTemplateByNameAsync(templateName);
                if (template == null || template.Channel != "SMS")
                {
                    _logger.LogError("SMS template not found: {TemplateName}", templateName);
                    return false;
                }

                // Render the template
                var message = await _templateService.RenderSMSTemplateAsync(templateName, variables);

                // Send the SMS
                return await SendSMSAsync(toPhoneNumber, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending templated SMS to {PhoneNumber} with template {TemplateName}",
                    toPhoneNumber, templateName);
                return false;
            }
        }

        public async Task<bool> SendVerificationSMSAsync(string toPhoneNumber, string verificationCode)
        {
            var message = $"Your WeStay verification code is: {verificationCode}. This code will expire in 10 minutes.";

            return await SendSMSAsync(toPhoneNumber, message);
        }

        public async Task<bool> SendBookingConfirmationSMSAsync(string toPhoneNumber, string bookingCode, DateTime checkInDate)
        {
            var message = $"Your WeStay booking {bookingCode} is confirmed! Check-in: {checkInDate:MMM dd, yyyy}. Thank you for choosing WeStay!";

            return await SendSMSAsync(toPhoneNumber, message);
        }

        public async Task<bool> SendBookingReminderSMSAsync(string toPhoneNumber, string bookingCode, DateTime checkInDate)
        {
            var message = $"Reminder: Your WeStay booking {bookingCode} check-in is on {checkInDate:MMM dd, yyyy}. We look forward to hosting you!";

            return await SendSMSAsync(toPhoneNumber, message);
        }

        public async Task<bool> SendPaymentNotificationSMSAsync(string toPhoneNumber, string bookingCode, decimal amount)
        {
            var message = $"Payment received for booking {bookingCode}: ${amount}. Thank you for your payment!";

            return await SendSMSAsync(toPhoneNumber, message);
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            // Basic phone number validation - you might want to use a more robust library
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove any non-digit characters except '+'
            var cleanedNumber = new string(phoneNumber.Where(c => char.IsDigit(c) || c == '+').ToArray());

            // Check if it's a valid length (minimum 10 digits for US numbers)
            return cleanedNumber.Length >= 10 && cleanedNumber.Length <= 15;
        }

        public async Task<string> FormatPhoneNumberAsync(string phoneNumber)
        {
            try
            {
                // This would use Twilio's Lookup API to validate and format the number
                // For now, we'll do basic formatting
                if (phoneNumber.StartsWith("+"))
                    return phoneNumber;

                if (phoneNumber.StartsWith("00"))
                    return "+" + phoneNumber[2..];

                if (phoneNumber.StartsWith("0") && phoneNumber.Length > 1)
                    phoneNumber = phoneNumber[1..];

                // Assume US numbers for now
                if (phoneNumber.Length == 10)
                    return "+1" + phoneNumber;

                return "+" + phoneNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting phone number: {PhoneNumber}", phoneNumber);
                return phoneNumber; // Return original if formatting fails
            }
        }

        public async Task<bool> ValidatePhoneNumberAsync(string phoneNumber)
        {
            if (!_twilioInitialized)
                return IsValidPhoneNumber(phoneNumber);

            try
            {
                // Twilio Phone Number Lookup API would be used here
                // For now, use basic validation
                return IsValidPhoneNumber(phoneNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating phone number: {PhoneNumber}", phoneNumber);
                return false;
            }
        }
    }
}