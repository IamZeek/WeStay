using Microsoft.EntityFrameworkCore;
using WeStay.MessagingService.Data;
using WeStay.MessagingService.Models;
using WeStay.MessagingService.Repositories.Interfaces;

namespace WeStay.MessagingService.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly MessagingDbContext _context;
        private readonly ILogger<MessageRepository> _logger;

        public MessageRepository(MessagingDbContext context, ILogger<MessageRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Message> GetMessageByIdAsync(int id)
        {
            return await _context.Messages
                .Include(m => m.Conversation)
                .Include(m => m.MessageReads)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<Message>> GetConversationMessagesAsync(int conversationId, int page = 1, int pageSize = 50)
        {
            return await _context.Messages
                .Include(m => m.MessageReads)
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetUnreadMessagesAsync(int conversationId, int userId)
        {
            var lastRead = await _context.ConversationParticipants
                .Where(cp => cp.ConversationId == conversationId && cp.UserId == userId)
                .Select(cp => cp.LastReadAt)
                .FirstOrDefaultAsync();

            return await _context.Messages
                .Where(m => m.ConversationId == conversationId &&
                          m.CreatedAt > (lastRead ?? DateTime.MinValue) &&
                          m.SenderId != userId &&
                          !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<Message> CreateMessageAsync(Message message)
        {
            message.CreatedAt = DateTime.UtcNow;

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Update conversation's updated timestamp
            var conversation = await _context.Conversations.FindAsync(message.ConversationId);
            if (conversation != null)
            {
                conversation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Created message {MessageId} in conversation {ConversationId}",
                message.Id, message.ConversationId);

            return message;
        }

        public async Task<Message> UpdateMessageAsync(Message message)
        {
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;

            _context.Messages.Update(message);
            await _context.SaveChangesAsync();

            return message;
        }

        public async Task<bool> DeleteMessageAsync(int id)
        {
            var message = await GetMessageByIdAsync(id);
            if (message == null) return false;

            message.IsDeleted = true;
            message.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkMessageAsReadAsync(int messageId, int userId)
        {
            var existingRead = await _context.MessageReads
                .FirstOrDefaultAsync(mr => mr.MessageId == messageId && mr.UserId == userId);

            if (existingRead != null)
            {
                existingRead.ReadAt = DateTime.UtcNow;
            }
            else
            {
                var messageRead = new MessageRead
                {
                    MessageId = messageId,
                    UserId = userId,
                    ReadAt = DateTime.UtcNow
                };
                _context.MessageReads.Add(messageRead);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(int conversationId, int userId)
        {
            var unreadMessages = await GetUnreadMessagesAsync(conversationId, userId);

            foreach (var message in unreadMessages)
            {
                await MarkMessageAsReadAsync(message.Id, userId);
            }

            // Update participant's last read timestamp
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

            if (participant != null)
            {
                participant.LastReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<int> GetMessageCountAsync(int conversationId)
        {
            return await _context.Messages
                .CountAsync(m => m.ConversationId == conversationId && !m.IsDeleted);
        }
    }
}