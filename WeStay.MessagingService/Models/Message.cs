using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeStay.MessagingService.Models
{
    public class Conversation
    {
        [Key]
        public int Id { get; set; }

        public Guid ConversationGuid { get; set; } = Guid.NewGuid();

        [Required]
        public int TypeId { get; set; }

        [ForeignKey("TypeId")]
        public virtual ConversationType Type { get; set; }

        public int? ListingId { get; set; }

        public int? BookingId { get; set; }

        [MaxLength(200)]
        public string Title { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsArchived { get; set; } = false;

        // Navigation properties
        public virtual ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }

    public class ConversationType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ConversationParticipant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }

        [Required]
        public int UserId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastReadAt { get; set; }

        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; }
    }

    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public string Content { get; set; }

        [MaxLength(20)]
        public string MessageType { get; set; } = "text"; // text, image, file, system

        [MaxLength(500)]
        public string FileUrl { get; set; }

        [MaxLength(255)]
        public string FileName { get; set; }

        public int? FileSize { get; set; }

        public bool IsEdited { get; set; } = false;

        public DateTime? EditedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; }

        public virtual ICollection<MessageRead> MessageReads { get; set; } = new List<MessageRead>();
    }

    public class MessageRead
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MessageId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime ReadAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("MessageId")]
        public virtual Message Message { get; set; }
    }

    // Enums for type safety
    public enum MessageType
    {
        Text,
        Image,
        File,
        System
    }

    public enum ConversationTypeEnum
    {
        Direct = 1,
        Booking = 2,
        Support = 3,
        Group = 4
    }
}