using Microsoft.EntityFrameworkCore;
using WeStay.NotificationService.Data;
using WeStay.NotificationService.Models;
using WeStay.NotificationService.Repositories.Interfaces;

namespace WeStay.NotificationService.Repositories
{
    public class NotificationTypeRepository : INotificationTypeRepository
    {
        private readonly NotificationDbContext _context;

        public NotificationTypeRepository(NotificationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<NotificationType>> GetAllTypesAsync()
        {
            return await _context.NotificationTypes
                .Where(nt => nt.IsActive)
                .ToListAsync();
        }

        public async Task<NotificationType> GetTypeByIdAsync(int id)
        {
            return await _context.NotificationTypes.FindAsync(id);
        }

        public async Task<NotificationType> GetTypeByNameAsync(string name)
        {
            return await _context.NotificationTypes
                .FirstOrDefaultAsync(nt => nt.Name == name && nt.IsActive);
        }
    }
}