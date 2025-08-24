using System.ComponentModel.DataAnnotations;

namespace WeStay.NotificationService.DTOs
{
    public class SendNotificationRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } // BookingConfirmation, PasswordReset, etc.

        [Required]
        [MaxLength(20)]
        public string Channel { get; set; } // Email, SMS, Push, InApp

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; }

        [Required]
        public string Message { get; set; }

        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

        [Range(0, 2)]
        public int Priority { get; set; } = 0; // 0: Low, 1: Medium, 2: High
    }

    public class SendTemplatedNotificationRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string TemplateName { get; set; }

        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

        [Range(0, 2)]
        public int Priority { get; set; } = 0;
    }

    public class NotificationResponse
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Channel { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public bool IsSent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class UpdatePreferencesRequest
    {
        public bool? EmailEnabled { get; set; }
        public bool? SMSEnabled { get; set; }
        public bool? PushEnabled { get; set; }
        public bool? MarketingEmails { get; set; }
        public bool? BookingNotifications { get; set; }
        public bool? SecurityNotifications { get; set; }
        public bool? Newsletter { get; set; }
    }
}