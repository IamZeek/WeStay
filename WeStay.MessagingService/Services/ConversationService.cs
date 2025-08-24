using WeStay.MessagingService.DTOs;
using WeStay.MessagingService.Models;
using WeStay.MessagingService.Repositories.Interfaces;
using WeStay.MessagingService.Services.Interfaces;

namespace WeStay.MessagingService.Services
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IConversationTypeRepository _typeRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly ILogger<ConversationService> _logger;
        private readonly HttpClient _httpClient;

        public ConversationService(
            IConversationRepository conversationRepository,
            IConversationTypeRepository typeRepository,
            IMessageRepository messageRepository,
            ILogger<ConversationService> logger,
            HttpClient httpClient)
        {
            _conversationRepository = conversationRepository;
            _typeRepository = typeRepository;
            _messageRepository = messageRepository;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<Conversation> GetConversationByIdAsync(int id)
        {
            return await _conversationRepository.GetConversationByIdAsync(id);
        }

        public async Task<Conversation> GetConversationByGuidAsync(Guid guid)
        {
            return await _conversationRepository.GetConversationByGuidAsync(guid);
        }

        public async Task<IEnumerable<ConversationResponse>> GetUserConversationsAsync(int userId)
        {
            var conversations = await _conversationRepository.GetUserConversationsAsync(userId);
            var result = new List<ConversationResponse>();

            foreach (var conversation in conversations)
            {
                var unreadCount = await _conversationRepository.GetUnreadCountAsync(conversation.Id, userId);
                var lastMessage = conversation.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();

                var participants = new List<ParticipantResponse>();
                foreach (var participant in conversation.Participants.Where(p => p.IsActive))
                {
                    var userInfo = await GetUserInfoAsync(participant.UserId);
                    participants.Add(new ParticipantResponse
                    {
                        UserId = participant.UserId,
                        UserName = userInfo?.UserName ?? "Unknown User",
                        Email = userInfo?.Email ?? "",
                        ProfilePicture = userInfo?.ProfilePicture,
                        LastReadAt = participant.LastReadAt,
                        IsActive = participant.IsActive
                    });
                }

                result.Add(new ConversationResponse
                {
                    Id = conversation.Id,
                    ConversationGuid = conversation.ConversationGuid,
                    Type = conversation.Type.Name,
                    Title = conversation.Title,
                    ListingId = conversation.ListingId,
                    BookingId = conversation.BookingId,
                    UpdatedAt = conversation.UpdatedAt,
                    UnreadCount = unreadCount,
                    LastMessage = lastMessage != null ? MapToMessageResponse(lastMessage, userId) : null,
                    Participants = participants
                });
            }

            return result;
        }

        public async Task<Conversation> CreateConversationAsync(CreateConversationRequest request)
        {
            // Validate conversation type
            var conversationType = await _typeRepository.GetTypeByIdAsync(request.TypeId);
            if (conversationType == null)
            {
                throw new ArgumentException("Invalid conversation type");
            }

            // Validate participants
            if (request.ParticipantIds.Count < 2 && conversationType.Name != "Support")
            {
                throw new ArgumentException("Conversation must have at least 2 participants");
            }

            var conversation = new Conversation
            {
                TypeId = request.TypeId,
                ListingId = request.ListingId,
                BookingId = request.BookingId,
                Title = request.Title,
                Participants = request.ParticipantIds.Select(userId => new ConversationParticipant
                {
                    UserId = userId,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow
                }).ToList()
            };

            return await _conversationRepository.CreateConversationAsync(conversation);
        }

        public async Task<bool> AddParticipantAsync(int conversationId, int userId)
        {
            return await _conversationRepository.AddParticipantAsync(conversationId, userId);
        }

        public async Task<bool> RemoveParticipantAsync(int conversationId, int userId)
        {
            return await _conversationRepository.RemoveParticipantAsync(conversationId, userId);
        }

        public async Task<bool> ArchiveConversationAsync(int conversationId)
        {
            return await _conversationRepository.ArchiveConversationAsync(conversationId);
        }

        public async Task<bool> IsUserParticipantAsync(int conversationId, int userId)
        {
            return await _conversationRepository.IsUserParticipantAsync(conversationId, userId);
        }

        public async Task<int> GetUnreadCountAsync(int conversationId, int userId)
        {
            return await _conversationRepository.GetUnreadCountAsync(conversationId, userId);
        }

        public async Task<bool> MarkConversationAsReadAsync(int conversationId, int userId)
        {
            return await _conversationRepository.UpdateLastReadAsync(conversationId, userId);
        }

        private async Task<UserInfo> GetUserInfoAsync(int userId)
        {
            try
            {
                // This would call the AuthService to get user information
                // For now, return mock data
                return new UserInfo
                {
                    UserId = userId,
                    UserName = $"User{userId}",
                    Email = $"user{userId}@example.com",
                    ProfilePicture = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info for user {UserId}", userId);
                return null;
            }
        }

        private MessageResponse MapToMessageResponse(Message message, int currentUserId)
        {
            return new MessageResponse
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                SenderName = $"User{message.SenderId}", // Would come from user service
                Content = message.Content,
                MessageType = message.MessageType,
                FileUrl = message.FileUrl,
                FileName = message.FileName,
                FileSize = message.FileSize,
                IsEdited = message.IsEdited,
                EditedAt = message.EditedAt,
                CreatedAt = message.CreatedAt,
                IsRead = message.MessageReads.Any(mr => mr.UserId == currentUserId),
                ReadAt = message.MessageReads.FirstOrDefault(mr => mr.UserId == currentUserId)?.ReadAt
            };
        }

        private class UserInfo
        {
            public int UserId { get; set; }
            public string UserName { get; set; }
            public string Email { get; set; }
            public string ProfilePicture { get; set; }
        }
    }
}