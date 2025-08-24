using Microsoft.EntityFrameworkCore;
using WeStay.MessagingService.Data;
using WeStay.MessagingService.Models;
using WeStay.MessagingService.Repositories.Interfaces;

namespace WeStay.MessagingService.Repositories
{
    public class ConversationTypeRepository : IConversationTypeRepository
    {
        private readonly MessagingDbContext _context;

        public ConversationTypeRepository(MessagingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ConversationType>> GetAllTypesAsync()
        {
            return await _context.ConversationTypes.ToListAsync();
        }

        public async Task<ConversationType> GetTypeByIdAsync(int id)
        {
            return await _context.ConversationTypes.FindAsync(id);
        }

        public async Task<ConversationType> GetTypeByNameAsync(string name)
        {
            return await _context.ConversationTypes
                .FirstOrDefaultAsync(ct => ct.Name == name);
        }
    }
}