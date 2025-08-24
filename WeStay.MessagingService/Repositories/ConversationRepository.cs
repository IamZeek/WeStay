using Microsoft.EntityFrameworkCore;
using WeStay.MessagingService.Data;
using WeStay.MessagingService.Models;
using WeStay.MessagingService.Repositories.Interfaces;

namespace WeStay.MessagingService.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly MessagingDbContext _context;
        private readonly ILogger<ConversationRepository> _logger;

        public ConversationRepository(MessagingDbContext context, ILogger<ConversationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Conversation> GetConversationByIdAsync(int id)
        {
            return await _context.Conversations
                .Include(c => c.Type)
                .Include(c => c.Participants)
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Conversation> GetConversationByGuidAsync(Guid guid)
        {
            return await _context.Conversations
                .Include(c => c.Type)
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.ConversationGuid == guid);
        }

        public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(int userId)
        {
            return await _context.Conversations
                .Include(c => c.Type)
                .Include(c => c.Participants)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .Where(c => c.Participants.Any(p => p.UserId == userId && p.IsActive))
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conversation>> GetConversationsByListingAsync(int listingId)
        {
            return await _context.Conversations
                .Include(c => c.Type)
                .Include(c => c.Participants)
                .Where(c => c.ListingId == listingId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conversation>> GetConversationsByBookingAsync(int bookingId)
        {
            return await _context.Conversations
                .Include(c => c.Type)
                .Include(c => c.Participants)
                .Where(c => c.BookingId == bookingId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }

        public async Task<Conversation> CreateConversationAsync(Conversation conversation)
        {
            conversation.CreatedAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created conversation {ConversationId} with GUID {ConversationGuid}",
                conversation.Id, conversation.ConversationGuid);

            return conversation;
        }

        public async Task<Conversation> UpdateConversationAsync(Conversation conversation)
        {
            conversation.UpdatedAt = DateTime.UtcNow;
            _context.Conversations.Update(conversation);
            await _context.SaveChangesAsync();

            return conversation;
        }

        public async Task<bool> DeleteConversationAsync(int id)
        {
            var conversation = await GetConversationByIdAsync(id);
            if (conversation == null) return false;

            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted conversation {ConversationId}", id);
            return true;
        }

        public async Task<bool> ArchiveConversationAsync(int id)
        {
            var conversation = await GetConversationByIdAsync(id);
            if (conversation == null) return false;

            conversation.IsArchived = true;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsUserParticipantAsync(int conversationId, int userId)
        {
            return await _context.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId && cp.IsActive);
        }

        public async Task<int> GetUnreadCountAsync(int conversationId, int userId)
        {
            var lastRead = await _context.ConversationParticipants
                .Where(cp => cp.ConversationId == conversationId && cp.UserId == userId)
                .Select(cp => cp.LastReadAt)
                .FirstOrDefaultAsync();

            return await _context.Messages
                .CountAsync(m => m.ConversationId == conversationId &&
                               m.CreatedAt > (lastRead ?? DateTime.MinValue) &&
                               m.SenderId != userId);
        }

        public async Task<bool> AddParticipantAsync(int conversationId, int userId)
        {
            var existingParticipant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

            if (existingParticipant != null)
            {
                if (existingParticipant.IsActive) return true;

                existingParticipant.IsActive = true;
                existingParticipant.JoinedAt = DateTime.UtcNow;
            }
            else
            {
                var participant = new ConversationParticipant
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow
                };
                _context.ConversationParticipants.Add(participant);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveParticipantAsync(int conversationId, int userId)
        {
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

            if (participant == null) return false;

            participant.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateLastReadAsync(int conversationId, int userId)
        {
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

            if (participant == null) return false;

            participant.LastReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}