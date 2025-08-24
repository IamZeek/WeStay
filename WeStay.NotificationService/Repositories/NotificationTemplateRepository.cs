using Microsoft.EntityFrameworkCore;
using WeStay.NotificationService.Data;
using WeStay.NotificationService.Models;
using WeStay.NotificationService.Repositories.Interfaces;

namespace WeStay.NotificationService.Repositories
{
    public class NotificationTemplateRepository : INotificationTemplateRepository
    {
        private readonly NotificationDbContext _context;
        private readonly ILogger<NotificationTemplateRepository> _logger;

        public NotificationTemplateRepository(NotificationDbContext context, ILogger<NotificationTemplateRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<NotificationTemplate>> GetAllTemplatesAsync()
        {
            return await _context.NotificationTemplates
                .Where(nt => nt.IsActive)
                .ToListAsync();
        }

        public async Task<NotificationTemplate> GetTemplateByIdAsync(int id)
        {
            return await _context.NotificationTemplates.FindAsync(id);
        }

        public async Task<NotificationTemplate> GetTemplateByNameAsync(string name)
        {
            return await _context.NotificationTemplates
                .FirstOrDefaultAsync(nt => nt.Name == name && nt.IsActive);
        }

        public async Task<NotificationTemplate> GetTemplateByTypeAndChannelAsync(string type, string channel)
        {
            return await _context.NotificationTemplates
                .FirstOrDefaultAsync(nt => nt.Name.StartsWith(type) && nt.Channel == channel && nt.IsActive);
        }

        public async Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template)
        {
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;

            _context.NotificationTemplates.Add(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created notification template {TemplateName}", template.Name);

            return template;
        }

        public async Task<NotificationTemplate> UpdateTemplateAsync(NotificationTemplate template)
        {
            template.UpdatedAt = DateTime.UtcNow;
            _context.NotificationTemplates.Update(template);
            await _context.SaveChangesAsync();

            return template;
        }

        public async Task<bool> DeleteTemplateAsync(int id)
        {
            var template = await GetTemplateByIdAsync(id);
            if (template == null) return false;

            template.IsActive = false;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}