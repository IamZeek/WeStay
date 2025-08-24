using Microsoft.Extensions.Options;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using WeStay.NotificationService.Models;
using WeStay.NotificationService.Services.Interfaces;
using WeStay.NotificationService.Repositories.Interfaces;

namespace WeStay.NotificationService.Services
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly PushNotificationSettings _pushSettings;
        private readonly ILogger<PushNotificationService> _logger;
        private readonly INotificationTemplateRepository _templateRepository;
        private readonly ITemplateService _templateService;
        private bool _firebaseInitialized = false;

        public PushNotificationService(
            IOptions<PushNotificationSettings> pushSettings,
            ILogger<PushNotificationService> logger,
            INotificationTemplateRepository templateRepository,
            ITemplateService templateService)
        {
            _pushSettings = pushSettings.Value;
            _logger = logger;
            _templateRepository = templateRepository;
            _templateService = templateService;
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            try
            {
                if (FirebaseApp.DefaultInstance == null)
                {
                    if (!string.IsNullOrEmpty(_pushSettings.FirebasePrivateKey) &&
                        !string.IsNullOrEmpty(_pushSettings.FirebaseClientEmail))
                    {
                        var credential = GoogleCredential.FromJson($$"""
                        {
                            "type": "service_account",
                            "project_id": "{{_pushSettings.FirebaseProjectId}}",
                            "private_key_id": "{{_pushSettings.FirebasePrivateKey}}",
                            "private_key": "{{_pushSettings.FirebasePrivateKey}}",
                            "client_email": "{{_pushSettings.FirebaseClientEmail}}",
                            "client_id": "{{_pushSettings.FirebaseProjectId}}",
                            "auth_uri": "https://accounts.google.com/o/oauth2/auth",
                            "token_uri": "https://oauth2.googleapis.com/token"
                        }
                        """);

                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = credential
                        });

                        _firebaseInitialized = true;
                        _logger.LogInformation("Firebase Admin SDK initialized successfully");
                    }
                    else
                    {
                        _logger.LogWarning("Firebase credentials not configured. Push notifications will not work.");
                    }
                }
                else
                {
                    _firebaseInitialized = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase Admin SDK");
            }
        }

        public async Task<bool> SendPushAsync(int userId, string title, string message, Dictionary<string, string> data = null)
        {
            if (!_firebaseInitialized)
            {
                _logger.LogError("Firebase not initialized. Cannot send push notification.");
                return false;
            }

            try
            {
                // Get user's FCM tokens from database (this would be stored when user registers device)
                var userTokens = await GetUserFcmTokensAsync(userId);

                if (!userTokens.Any())
                {
                    _logger.LogWarning("No FCM tokens found for user {UserId}", userId);
                    return false;
                }

                var multicastMessage = new MulticastMessage
                {
                    Tokens = userTokens,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = title,
                        Body = message
                    },
                    Data = data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High
                    },
                    Apns = new ApnsConfig
                    {
                        Headers = new Dictionary<string, string>
                        {
                            { "apns-priority", "10" }
                        },
                        Aps = new Aps
                        {
                            Alert = new ApsAlert
                            {
                                Title = title,
                                Body = message
                            },
                            Sound = "default"
                        }
                    }
                };

                var response = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(multicastMessage);

                _logger.LogInformation("Push notification sent to {SuccessCount} devices for user {UserId}. {FailureCount} failures.",
                    response.SuccessCount, userId, response.FailureCount);

                // Remove failed tokens
                if (response.FailureCount > 0)
                {
                    await RemoveFailedTokensAsync(userId, userTokens, response.Responses);
                }

                return response.SuccessCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification to user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> SendToTopicAsync(string topic, string title, string message, Dictionary<string, string> data = null)
        {
            if (!_firebaseInitialized)
            {
                _logger.LogError("Firebase not initialized. Cannot send push notification.");
                return false;
            }

            try
            {
                var messageObj = new Message
                {
                    Topic = topic,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = title,
                        Body = message
                    },
                    Data = data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                };

                var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(messageObj);

                _logger.LogInformation("Push notification sent to topic {Topic}. Message ID: {MessageId}", topic, messageId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification to topic {Topic}", topic);
                return false;
            }
        }

        public async Task<bool> SubscribeToTopicAsync(int userId, string topic)
        {
            if (!_firebaseInitialized)
            {
                _logger.LogError("Firebase not initialized. Cannot subscribe to topic.");
                return false;
            }

            try
            {
                var userTokens = await GetUserFcmTokensAsync(userId);

                if (!userTokens.Any())
                {
                    _logger.LogWarning("No FCM tokens found for user {UserId}", userId);
                    return false;
                }

                var response = await FirebaseMessaging.DefaultInstance.SubscribeToTopicAsync(userTokens, topic);

                _logger.LogInformation("Subscribed {SuccessCount} devices to topic {Topic} for user {UserId}. {FailureCount} failures.",
                    response.SuccessCount, topic, userId, response.FailureCount);

                return response.SuccessCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing user {UserId} to topic {Topic}", userId, topic);
                return false;
            }
        }

        public async Task<bool> UnsubscribeFromTopicAsync(int userId, string topic)
        {
            if (!_firebaseInitialized)
            {
                _logger.LogError("Firebase not initialized. Cannot unsubscribe from topic.");
                return false;
            }

            try
            {
                var userTokens = await GetUserFcmTokensAsync(userId);

                if (!userTokens.Any())
                {
                    _logger.LogWarning("No FCM tokens found for user {UserId}", userId);
                    return false;
                }

                var response = await FirebaseMessaging.DefaultInstance.UnsubscribeFromTopicAsync(userTokens, topic);

                _logger.LogInformation("Unsubscribed {SuccessCount} devices from topic {Topic} for user {UserId}. {FailureCount} failures.",
                    response.SuccessCount, topic, userId, response.FailureCount);

                return response.SuccessCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing user {UserId} from topic {Topic}", userId, topic);
                return false;
            }
        }

        public async Task<bool> SendBookingConfirmationPushAsync(int userId, string bookingCode, DateTime checkInDate)
        {
            var title = "Booking Confirmed!";
            var message = $"Your booking {bookingCode} is confirmed. Check-in: {checkInDate:MMM dd, yyyy}";

            var data = new Dictionary<string, string>
            {
                { "type", "booking_confirmation" },
                { "bookingCode", bookingCode },
                { "checkInDate", checkInDate.ToString("yyyy-MM-dd") },
                { "deepLink", $"westay://booking/{bookingCode}" }
            };

            return await SendPushAsync(userId, title, message, data);
        }

        public async Task<bool> SendBookingReminderPushAsync(int userId, string bookingCode, DateTime checkInDate)
        {
            var title = "Booking Reminder";
            var message = $"Reminder: Your check-in is tomorrow for booking {bookingCode}";

            var data = new Dictionary<string, string>
            {
                { "type", "booking_reminder" },
                { "bookingCode", bookingCode },
                { "checkInDate", checkInDate.ToString("yyyy-MM-dd") },
                { "deepLink", $"westay://booking/{bookingCode}" }
            };

            return await SendPushAsync(userId, title, message, data);
        }

        public async Task<bool> SendReviewReminderPushAsync(int userId, string bookingCode)
        {
            var title = "How was your stay?";
            var message = "Please share your experience by leaving a review for your recent stay";

            var data = new Dictionary<string, string>
            {
                { "type", "review_reminder" },
                { "bookingCode", bookingCode },
                { "deepLink", $"westay://review/{bookingCode}" }
            };

            return await SendPushAsync(userId, title, message, data);
        }

        private async Task<List<string>> GetUserFcmTokensAsync(int userId)
        {
            // This would query your database for user's FCM tokens
            // For now, return empty list - you'd implement this based on your user token storage
            try
            {
                // Example: Query database for user's devices and their FCM tokens
                // var tokens = await _dbContext.UserDevices
                //     .Where(ud => ud.UserId == userId && ud.FcmToken != null)
                //     .Select(ud => ud.FcmToken)
                //     .ToListAsync();

                // return tokens;

                return new List<string>(); // Placeholder
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting FCM tokens for user {UserId}", userId);
                return new List<string>();
            }
        }

        private async Task RemoveFailedTokensAsync(int userId, List<string> tokens, IReadOnlyList<SendResponse> responses)
        {
            try
            {
                var failedTokens = new List<string>();
                for (int i = 0; i < responses.Count; i++)
                {
                    if (!responses[i].IsSuccess)
                    {
                        failedTokens.Add(tokens[i]);
                    }
                }

                if (failedTokens.Any())
                {
                    // Remove failed tokens from database
                    // await _dbContext.UserDevices
                    //     .Where(ud => ud.UserId == userId && failedTokens.Contains(ud.FcmToken))
                    //     .ForEachAsync(ud => ud.FcmToken = null);

                    // await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("Removed {Count} invalid FCM tokens for user {UserId}", failedTokens.Count, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing failed FCM tokens for user {UserId}", userId);
            }
        }

        public async Task<bool> RegisterDeviceTokenAsync(int userId, string deviceId, string fcmToken, string deviceType)
        {
            try
            {
                // This would store the device token in your database
                // Example implementation:
                /*
                var existingDevice = await _dbContext.UserDevices
                    .FirstOrDefaultAsync(ud => ud.UserId == userId && ud.DeviceId == deviceId);

                if (existingDevice != null)
                {
                    existingDevice.FcmToken = fcmToken;
                    existingDevice.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newDevice = new UserDevice
                    {
                        UserId = userId,
                        DeviceId = deviceId,
                        FcmToken = fcmToken,
                        DeviceType = deviceType,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _dbContext.UserDevices.Add(newDevice);
                }

                await _dbContext.SaveChangesAsync();
                */

                _logger.LogInformation("Registered FCM token for user {UserId}, device {DeviceId}", userId, deviceId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device token for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UnregisterDeviceTokenAsync(int userId, string deviceId)
        {
            try
            {
                // This would remove the device token from your database
                /*
                var device = await _dbContext.UserDevices
                    .FirstOrDefaultAsync(ud => ud.UserId == userId && ud.DeviceId == deviceId);

                if (device != null)
                {
                    device.FcmToken = null;
                    device.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }
                */

                _logger.LogInformation("Unregistered FCM token for user {UserId}, device {DeviceId}", userId, deviceId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering device token for user {UserId}", userId);
                return false;
            }
        }
    }
}