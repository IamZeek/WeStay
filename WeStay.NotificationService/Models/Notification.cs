using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeStay.NotificationService.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TypeId { get; set; }

        [ForeignKey("TypeId")]
        public virtual NotificationType Type { get; set; }

        [Required]
        public int UserId { get; set; } // Recipient user ID

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; }

        [Required]
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public bool IsSent { get; set; } = false;

        public DateTime? SentAt { get; set; }

        public DateTime? ReadAt { get; set; }

        [Range(0, 2)]
        public int Priority { get; set; } = 0; // 0: Low, 1: Medium, 2: High

        [Required]
        [MaxLength(20)]
        public string Channel { get; set; } // Email, SMS, Push, InApp

        [MaxLength(255)]
        public string ExternalId { get; set; } // ID from external service

        [MaxLength(500)]
        public string ErrorMessage { get; set; }

        public int RetryCount { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class NotificationType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        [MaxLength(200)]
        public string TemplateSubject { get; set; }

        public string TemplateBody { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }

    public class NotificationTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        [Required]
        [MaxLength(200)]
        public string SubjectTemplate { get; set; }

        [Required]
        public string BodyTemplate { get; set; }

        [MaxLength(10)]
        public string Language { get; set; } = "en";

        [Required]
        [MaxLength(20)]
        public string Channel { get; set; } // Email, SMS, Push, InApp

        public string Variables { get; set; } // JSON array of template variables

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class UserNotificationPreferences
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public bool EmailEnabled { get; set; } = true;

        public bool SMSEnabled { get; set; } = false;

        public bool PushEnabled { get; set; } = true;

        public bool MarketingEmails { get; set; } = true;

        public bool BookingNotifications { get; set; } = true;

        public bool SecurityNotifications { get; set; } = true;

        public bool Newsletter { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // Enums for better type safety
    public enum NotificationChannel
    {
        Email,
        SMS,
        Push,
        InApp
    }

    public enum NotificationPriority
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public enum NotificationTypeEnum
    {
        BookingConfirmation = 1,
        BookingCancellation = 2,
        PaymentReceived = 3,
        PaymentFailed = 4,
        ReviewReminder = 5,
        SecurityAlert = 6,
        PasswordReset = 7,
        Welcome = 8
    }
}