using WeStay.NotificationService.DTOs;
using WeStay.NotificationService.Models;
using WeStay.NotificationService.Repositories.Interfaces;
using WeStay.NotificationService.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace WeStay.NotificationService.Services
{
    public class NotificationServices : INotificationServices
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationTypeRepository _typeRepository;
        private readonly IUserPreferencesRepository _preferencesRepository;
        private readonly INotificationTemplateRepository _templateRepository;
        private readonly IEmailService _emailService;
        private readonly ISMSService _smsService;
        private readonly IPushNotificationService _pushService;
        private readonly ITemplateService _templateService;
        private readonly ILogger<NotificationServices> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;


        public NotificationServices(
            INotificationRepository notificationRepository,
            INotificationTypeRepository typeRepository,
            IUserPreferencesRepository preferencesRepository,
            INotificationTemplateRepository templateRepository,
            IEmailService emailService,
            ISMSService smsService,
            IPushNotificationService pushService,
            ITemplateService templateService,
            ILogger<NotificationServices> logger,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _notificationRepository = notificationRepository;
            _typeRepository = typeRepository;
            _preferencesRepository = preferencesRepository;
            _emailService = emailService;
            _smsService = smsService;
            _pushService = pushService;
            _templateService = templateService;
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
            _templateRepository = templateRepository;

        }

        public async Task<Notification> SendNotificationAsync(SendNotificationRequest request)
        {
            try
            {
                // Check user preferences
                var isEnabled = await _preferencesRepository.AreNotificationsEnabledAsync(
                    request.UserId, request.Channel, request.Type);

                if (!isEnabled)
                {
                    _logger.LogInformation("Notifications disabled for user {UserId}, channel {Channel}, type {Type}",
                        request.UserId, request.Channel, request.Type);
                    return null;
                }

                var notificationType = await _typeRepository.GetTypeByNameAsync(request.Type);
                if (notificationType == null)
                {
                    throw new ArgumentException($"Invalid notification type: {request.Type}");
                }

                var notification = new Notification
                {
                    UserId = request.UserId,
                    TypeId = notificationType.Id,
                    Channel = request.Channel,
                    Subject = request.Subject,
                    Message = request.Message,
                    Priority = request.Priority
                };

                var createdNotification = await _notificationRepository.CreateNotificationAsync(notification);

                // Send immediately for high priority notifications
                if (request.Priority == 2) // High priority
                {
                    await SendNotificationImmediatelyAsync(createdNotification);
                }

                return createdNotification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", request.UserId);
                throw;
            }
        }

        public async Task<Notification> SendTemplatedNotificationAsync(SendTemplatedNotificationRequest request)
        {
            try
            {
                // Check user preferences (default to email channel for templated notifications)
                var isEnabled = await _preferencesRepository.AreNotificationsEnabledAsync(
                    request.UserId, "email", "templated");

                if (!isEnabled)
                {
                    _logger.LogInformation("Templated notifications disabled for user {UserId}", request.UserId);
                    return null;
                }

                // Determine channel from template name (e.g., "Welcome_Email" -> "email")
                var channel = GetChannelFromTemplateName(request.TemplateName);

                // Get user contact information based on channel
                string recipient = channel.ToLower() switch
                {
                    "email" => await GetUserEmailAsync(request.UserId),
                    "sms" => await GetUserPhoneAsync(request.UserId),
                    _ => null
                };

                if (string.IsNullOrEmpty(recipient))
                {
                    _logger.LogWarning("No recipient found for user {UserId} channel {Channel}", request.UserId, channel);
                    return null;
                }

                bool success = false;
                string subject = string.Empty;
                string message = string.Empty;

                // Send based on channel
                switch (channel.ToLower())
                {
                    case "email":
                        (subject, message) = await _templateService.RenderEmailTemplateAsync(request.TemplateName, request.Variables);
                        success = await _emailService.SendEmailAsync(recipient, subject, message);
                        break;

                    case "sms":
                        message = await _templateService.RenderSMSTemplateAsync(request.TemplateName, request.Variables);
                        success = await _smsService.SendSMSAsync(recipient, message);
                        break;

                    case "push":
                        // For push notifications, we need title and message
                        var pushTemplate = await _templateRepository.GetTemplateByNameAsync(request.TemplateName);
                        if (pushTemplate != null)
                        {
                            subject = RenderTemplateContent(pushTemplate.SubjectTemplate, request.Variables);
                            message = RenderTemplateContent(pushTemplate.BodyTemplate, request.Variables);
                            success = await _pushService.SendPushAsync(request.UserId, subject, message, request.Variables);
                        }
                        break;

                    default:
                        _logger.LogWarning("Unsupported channel {Channel} for template {TemplateName}", channel, request.TemplateName);
                        break;
                }

                // Create notification record
                var notificationType = await _typeRepository.GetTypeByNameAsync("Templated");
                var notification = new Notification
                {
                    UserId = request.UserId,
                    TypeId = notificationType?.Id ?? 1,
                    Channel = channel,
                    Subject = subject,
                    Message = message,
                    Priority = request.Priority,
                    IsSent = success
                };

                var createdNotification = await _notificationRepository.CreateNotificationAsync(notification);

                if (success)
                {
                    await _notificationRepository.MarkAsSentAsync(createdNotification.Id, $"templated-{DateTime.UtcNow.Ticks}");
                }

                return createdNotification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending templated notification to user {UserId}", request.UserId);
                throw;
            }
        }

        public async Task<bool> SendBookingConfirmationAsync(int userId, string bookingCode, DateTime checkInDate, DateTime checkOutDate)
        {
            try
            {
                var variables = new Dictionary<string, string>
                {
                    { "UserName", await GetUserNameAsync(userId) },
                    { "BookingCode", bookingCode },
                    { "CheckInDate", checkInDate.ToString("MMM dd, yyyy") },
                    { "CheckOutDate", checkOutDate.ToString("MMM dd, yyyy") },
                    { "Nights", (checkOutDate - checkInDate).Days.ToString() }
                };

                // Send email
                var emailEnabled = await _preferencesRepository.AreNotificationsEnabledAsync(userId, "email", "BookingConfirmation");
                if (emailEnabled)
                {
                    var email = await GetUserEmailAsync(userId);
                    if (!string.IsNullOrEmpty(email))
                    {
                        await _emailService.SendBookingConfirmationEmailAsync(email,
                            await GetUserNameAsync(userId), bookingCode, checkInDate, checkOutDate);
                    }
                }

                // Send SMS
                var smsEnabled = await _preferencesRepository.AreNotificationsEnabledAsync(userId, "sms", "BookingConfirmation");
                if (smsEnabled)
                {
                    var phone = await GetUserPhoneAsync(userId);
                    if (!string.IsNullOrEmpty(phone))
                    {
                        await _smsService.SendBookingConfirmationSMSAsync(phone, bookingCode, checkInDate);
                    }
                }

                // Send push notification
                var pushEnabled = await _preferencesRepository.AreNotificationsEnabledAsync(userId, "push", "BookingConfirmation");
                if (pushEnabled)
                {
                    await _pushService.SendBookingConfirmationPushAsync(userId, bookingCode, checkInDate);
                }

                // Create in-app notification
                var notification = new Notification
                {
                    UserId = userId,
                    TypeId = (await _typeRepository.GetTypeByNameAsync("BookingConfirmation"))?.Id ?? 1,
                    Channel = "inapp",
                    Subject = "Booking Confirmed",
                    Message = $"Your booking {bookingCode} has been confirmed. Check-in: {checkInDate:MMM dd, yyyy}",
                    Priority = 1, // Medium priority
                    IsSent = true // In-app notifications are always "sent"
                };

                await _notificationRepository.CreateNotificationAsync(notification);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending booking confirmation to user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetAsync(int userId, string resetToken)
        {
            try
            {
                var resetLink = $"{_configuration["Frontend:BaseUrl"]}/reset-password?token={resetToken}";
                var userName = await GetUserNameAsync(userId);
                var userEmail = await GetUserEmailAsync(userId);

                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("No email found for user {UserId} for password reset", userId);
                    return false;
                }

                // Send email
                var emailEnabled = await _preferencesRepository.AreNotificationsEnabledAsync(userId, "email", "PasswordReset");
                if (emailEnabled)
                {
                    await _emailService.SendPasswordResetEmailAsync(userEmail, resetLink, userName);
                }

                // Create notification record
                var notification = new Notification
                {
                    UserId = userId,
                    TypeId = (await _typeRepository.GetTypeByNameAsync("PasswordReset"))?.Id ?? 1,
                    Channel = "email",
                    Subject = "Password Reset Request",
                    Message = $"Password reset link: {resetLink}",
                    Priority = 2, // High priority for security-related notifications
                    IsSent = emailEnabled
                };

                await _notificationRepository.CreateNotificationAsync(notification);

                return emailEnabled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset to user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> SendWelcomeNotificationAsync(int userId)
        {
            try
            {
                var userName = await GetUserNameAsync(userId);
                var userEmail = await GetUserEmailAsync(userId);

                // Send welcome email
                var emailEnabled = await _preferencesRepository.AreNotificationsEnabledAsync(userId, "email", "Welcome");
                if (emailEnabled && !string.IsNullOrEmpty(userEmail))
                {
                    await _emailService.SendWelcomeEmailAsync(userEmail, userName);
                }

                // Send welcome push notification
                var pushEnabled = await _preferencesRepository.AreNotificationsEnabledAsync(userId, "push", "Welcome");
                if (pushEnabled)
                {
                    await _pushService.SendPushAsync(userId, "Welcome to WeStay!",
                        $"Hello {userName}! Welcome to WeStay. Start exploring properties now!");
                }

                // Create notification record
                var notification = new Notification
                {
                    UserId = userId,
                    TypeId = (await _typeRepository.GetTypeByNameAsync("Welcome"))?.Id ?? 1,
                    Channel = "system",
                    Subject = "Welcome to WeStay",
                    Message = "Thank you for joining WeStay! We're excited to have you.",
                    Priority = 0, // Low priority
                    IsSent = true
                };

                await _notificationRepository.CreateNotificationAsync(notification);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome notification to user {UserId}", userId);
                return false;
            }
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20)
        {
            return await _notificationRepository.GetNotificationsByUserIdAsync(userId, page, pageSize);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _notificationRepository.GetNotificationByIdAsync(notificationId);
            if (notification == null || notification.UserId != userId)
            {
                return false;
            }

            return await _notificationRepository.MarkAsReadAsync(notificationId);
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _notificationRepository.GetUnreadCountAsync(userId);
        }

        public async Task ProcessPendingNotificationsAsync()
        {
            var pendingNotifications = await _notificationRepository.GetPendingNotificationsAsync();

            foreach (var notification in pendingNotifications)
            {
                try
                {
                    await SendNotificationImmediatelyAsync(notification);
                    await _notificationRepository.MarkAsSentAsync(notification.Id, $"processed-{DateTime.UtcNow.Ticks}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing notification {NotificationId}", notification.Id);
                    await _notificationRepository.RecordErrorAsync(notification.Id, ex.Message);
                }
            }
        }

        public async Task<bool> UpdateUserPreferencesAsync(int userId, UpdatePreferencesRequest request)
        {
            try
            {
                var preferences = await _preferencesRepository.GetPreferencesByUserIdAsync(userId);

                if (preferences == null)
                {
                    preferences = new UserNotificationPreferences
                    {
                        UserId = userId
                    };
                    await _preferencesRepository.CreatePreferencesAsync(preferences);
                }

                // Update only the properties that are provided
                if (request.EmailEnabled.HasValue) preferences.EmailEnabled = request.EmailEnabled.Value;
                if (request.SMSEnabled.HasValue) preferences.SMSEnabled = request.SMSEnabled.Value;
                if (request.PushEnabled.HasValue) preferences.PushEnabled = request.PushEnabled.Value;
                if (request.MarketingEmails.HasValue) preferences.MarketingEmails = request.MarketingEmails.Value;
                if (request.BookingNotifications.HasValue) preferences.BookingNotifications = request.BookingNotifications.Value;
                if (request.SecurityNotifications.HasValue) preferences.SecurityNotifications = request.SecurityNotifications.Value;
                if (request.Newsletter.HasValue) preferences.Newsletter = request.Newsletter.Value;

                await _preferencesRepository.UpdatePreferencesAsync(preferences);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating preferences for user {UserId}", userId);
                return false;
            }
        }

        private async Task SendNotificationImmediatelyAsync(Notification notification)
        {
            try
            {
                bool success = false;
                string externalId = null;

                switch (notification.Channel.ToLower())
                {
                    case "email":
                        var email = await GetUserEmailAsync(notification.UserId);
                        if (!string.IsNullOrEmpty(email))
                        {
                            success = await _emailService.SendEmailAsync(email, notification.Subject, notification.Message);
                            externalId = success ? $"email-{DateTime.UtcNow.Ticks}" : null;
                        }
                        break;

                    case "sms":
                        var phone = await GetUserPhoneAsync(notification.UserId);
                        if (!string.IsNullOrEmpty(phone))
                        {
                            success = await _smsService.SendSMSAsync(phone, notification.Message);
                            externalId = success ? $"sms-{DateTime.UtcNow.Ticks}" : null;
                        }
                        break;

                    case "push":
                        success = await _pushService.SendPushAsync(notification.UserId, notification.Subject, notification.Message);
                        externalId = success ? $"push-{DateTime.UtcNow.Ticks}" : null;
                        break;

                    case "inapp":
                        // In-app notifications are already stored, just mark as sent
                        success = true;
                        externalId = $"inapp-{DateTime.UtcNow.Ticks}";
                        break;
                }

                if (success)
                {
                    await _notificationRepository.MarkAsSentAsync(notification.Id, externalId);
                }
                else
                {
                    await _notificationRepository.RecordErrorAsync(notification.Id, "Failed to send notification");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending immediate notification {NotificationId}", notification.Id);
                await _notificationRepository.RecordErrorAsync(notification.Id, ex.Message);
            }
        }

        private string GetChannelFromTemplateName(string templateName)
        {
            if (templateName.EndsWith("_Email", StringComparison.OrdinalIgnoreCase))
                return "email";
            if (templateName.EndsWith("_SMS", StringComparison.OrdinalIgnoreCase))
                return "sms";
            if (templateName.EndsWith("_Push", StringComparison.OrdinalIgnoreCase))
                return "push";

            return "email";
        }

        private string RenderTemplateContent(string template, Dictionary<string, string> variables)
        {
            if (string.IsNullOrEmpty(template) || variables == null)
                return template;

            foreach (var variable in variables)
            {
                template = template.Replace($"{{{{{variable.Key}}}}}", variable.Value);
            }

            return template;
        }

        private async Task<string> GetUserEmailAsync(int userId)
        {
            try
            {
                // This would call the AuthService to get user email
                var authServiceUrl = _configuration["Services:AuthService"];
                var response = await _httpClient.GetAsync($"{authServiceUrl}/api/auth/user/{userId}/email");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                _logger.LogWarning("Failed to get email for user {UserId}", userId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email for user {UserId}", userId);
                return null;
            }
        }

        private async Task<string> GetUserPhoneAsync(int userId)
        {
            try
            {
                // This would call the AuthService to get user phone
                var authServiceUrl = _configuration["Services:AuthService"];
                var response = await _httpClient.GetAsync($"{authServiceUrl}/api/auth/user/{userId}/phone");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                _logger.LogWarning("Failed to get phone number for user {UserId}", userId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting phone number for user {UserId}", userId);
                return null;
            }
        }

        private async Task<string> GetUserNameAsync(int userId)
        {
            try
            {
                // This would call the AuthService to get user name
                var authServiceUrl = _configuration["Services:AuthService"];
                var response = await _httpClient.GetAsync($"{authServiceUrl}/api/auth/user/{userId}/name");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                _logger.LogWarning("Failed to get name for user {UserId}", userId);
                return "Guest";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting name for user {UserId}", userId);
                return "Guest";
            }
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var unreadNotifications = await _notificationRepository.GetUnreadNotificationsAsync(userId);

                foreach (var notification in unreadNotifications)
                {
                    await _notificationRepository.MarkAsReadAsync(notification.Id);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _notificationRepository.GetNotificationByIdAsync(notificationId);
                if (notification == null || notification.UserId != userId)
                {
                    return false;
                }

                // Soft delete by marking as deleted (you would add an IsDeleted field to the Notification model)
                // notification.IsDeleted = true;
                // await _notificationRepository.UpdateNotificationAsync(notification);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId} for user {UserId}", notificationId, userId);
                return false;
            }
        }
    }
}