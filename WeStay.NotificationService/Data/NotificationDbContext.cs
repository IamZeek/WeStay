using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using WeStay.NotificationService.Models;

namespace WeStay.NotificationService.Data
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
        {
        }

        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationType> NotificationTypes { get; set; }
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
        public DbSet<UserNotificationPreferences> UserNotificationPreferences { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure relationships
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Type)
                .WithMany(nt => nt.Notifications)
                .HasForeignKey(n => n.TypeId);

            // Create indexes
            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.UserId);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.TypeId);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.IsSent);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.Channel);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.CreatedAt);

            modelBuilder.Entity<NotificationTemplate>()
                .HasIndex(nt => nt.Name);

            modelBuilder.Entity<NotificationTemplate>()
                .HasIndex(nt => nt.Channel);

            modelBuilder.Entity<UserNotificationPreferences>()
                .HasIndex(unp => unp.UserId)
                .IsUnique();

            // Seed data
            modelBuilder.Entity<NotificationType>().HasData(
                new NotificationType { Id = 1, Name = "BookingConfirmation", Description = "Booking confirmation notification", TemplateSubject = "Booking Confirmation - {{BookingCode}}", TemplateBody = "Dear {{UserName}}, your booking {{BookingCode}} has been confirmed.", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new NotificationType { Id = 2, Name = "BookingCancellation", Description = "Booking cancellation notification", TemplateSubject = "Booking Cancelled - {{BookingCode}}", TemplateBody = "Dear {{UserName}}, your booking {{BookingCode}} has been cancelled.", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new NotificationType { Id = 3, Name = "PaymentReceived", Description = "Payment confirmation notification", TemplateSubject = "Payment Received - {{BookingCode}}", TemplateBody = "Dear {{UserName}}, payment for booking {{BookingCode}} has been received.", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new NotificationType { Id = 4, Name = "PaymentFailed", Description = "Payment failure notification", TemplateSubject = "Payment Failed - {{BookingCode}}", TemplateBody = "Dear {{UserName}}, payment for booking {{BookingCode}} has failed.", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new NotificationType { Id = 5, Name = "ReviewReminder", Description = "Review reminder notification", TemplateSubject = "Review Your Stay - {{BookingCode}}", TemplateBody = "Dear {{UserName}}, how was your stay? Please leave a review.", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new NotificationType { Id = 6, Name = "SecurityAlert", Description = "Security alert notification", TemplateSubject = "Security Alert - {{Subject}}", TemplateBody = "Security alert: {{Message}}", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new NotificationType { Id = 7, Name = "PasswordReset", Description = "Password reset notification", TemplateSubject = "Password Reset Request", TemplateBody = "Click here to reset your password: {{ResetLink}}", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new NotificationType { Id = 8, Name = "Welcome", Description = "Welcome notification", TemplateSubject = "Welcome to WeStay!", TemplateBody = "Dear {{UserName}}, welcome to WeStay! Start exploring properties.", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );

            modelBuilder.Entity<NotificationTemplate>().HasData(
                new NotificationTemplate { Id = 1, Name = "BookingConfirmation_Email", Description = "Booking confirmation email", SubjectTemplate = "Booking Confirmed - {{BookingCode}}", BodyTemplate = "<h1>Booking Confirmed</h1><p>Dear {{UserName}},</p><p>Your booking {{BookingCode}} has been confirmed.</p><p>Check-in: {{CheckInDate}}</p><p>Check-out: {{CheckOutDate}}</p>", Channel = "Email", Variables = "[\"UserName\", \"BookingCode\", \"CheckInDate\", \"CheckOutDate\"]", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new NotificationTemplate { Id = 2, Name = "BookingConfirmation_SMS", Description = "Booking confirmation SMS", SubjectTemplate = "Booking Confirmed", BodyTemplate = "Your booking {{BookingCode}} is confirmed. Check-in: {{CheckInDate}}", Channel = "SMS", Variables = "[\"BookingCode\", \"CheckInDate\"]", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new NotificationTemplate { Id = 3, Name = "Welcome_Email", Description = "Welcome email", SubjectTemplate = "Welcome to WeStay!", BodyTemplate = "<h1>Welcome {{UserName}}!</h1><p>Thank you for joining WeStay.</p>", Channel = "Email", Variables = "[\"UserName\"]", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new NotificationTemplate { Id = 4, Name = "PasswordReset_Email", Description = "Password reset email", SubjectTemplate = "Reset Your Password", BodyTemplate = "<p>Click here to reset your password: <a href=\"{{ResetLink}}\">Reset Password</a></p>", Channel = "Email", Variables = "[\"ResetLink\"]", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );
        }
    }
}