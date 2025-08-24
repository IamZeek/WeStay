using System.ComponentModel.DataAnnotations;

namespace WeStay.MessagingService.DTOs
{
    public class CreateConversationRequest
    {
        [Required]
        public int TypeId { get; set; }

        [Required]
        public List<int> ParticipantIds { get; set; } = new List<int>();

        public int? ListingId { get; set; }

        public int? BookingId { get; set; }

        [MaxLength(200)]
        public string Title { get; set; }
    }

    public class SendMessageRequest
    {
        [Required]
        public int ConversationId { get; set; }

        [Required]
        [MaxLength(5000)]
        public string Content { get; set; }

        [MaxLength(20)]
        public string MessageType { get; set; } = "text";

        public IFormFile? File { get; set; }
    }

    public class ConversationResponse
    {
        public int Id { get; set; }
        public Guid ConversationGuid { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public int? ListingId { get; set; }
        public int? BookingId { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int UnreadCount { get; set; }
        public MessageResponse LastMessage { get; set; }
        public List<ParticipantResponse> Participants { get; set; } = new List<ParticipantResponse>();
    }

    public class MessageResponse
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public string MessageType { get; set; }
        public string FileUrl { get; set; }
        public string FileName { get; set; }
        public int? FileSize { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class ParticipantResponse
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime? LastReadAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class MarkAsReadRequest
    {
        [Required]
        public int MessageId { get; set; }
    }

    public class AddParticipantRequest
    {
        [Required]
        public int ConversationId { get; set; }

        [Required]
        public int UserId { get; set; }
    }

    public class RemoveParticipantRequest
    {
        [Required]
        public int ConversationId { get; set; }

        [Required]
        public int UserId { get; set; }
    }
}