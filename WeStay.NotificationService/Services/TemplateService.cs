using WeStay.NotificationService.Services.Interfaces;
using WeStay.NotificationService.Repositories.Interfaces;

namespace WeStay.NotificationService.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly INotificationTemplateRepository _templateRepository;
        private readonly ILogger<TemplateService> _logger;

        public TemplateService(INotificationTemplateRepository templateRepository, ILogger<TemplateService> logger)
        {
            _templateRepository = templateRepository;
            _logger = logger;
        }

        public async Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> variables)
        {
            var template = await _templateRepository.GetTemplateByNameAsync(templateName);
            if (template == null)
            {
                throw new ArgumentException($"Template not found: {templateName}");
            }

            return RenderTemplateContent(template.BodyTemplate, variables);
        }

        public async Task<(string Subject, string Body)> RenderEmailTemplateAsync(string templateName, Dictionary<string, string> variables)
        {
            var template = await _templateRepository.GetTemplateByNameAsync(templateName);
            if (template == null || template.Channel != "Email")
            {
                throw new ArgumentException($"Email template not found: {templateName}");
            }

            var subject = RenderTemplateContent(template.SubjectTemplate, variables);
            var body = RenderTemplateContent(template.BodyTemplate, variables);

            return (subject, body);
        }

        public async Task<string> RenderSMSTemplateAsync(string templateName, Dictionary<string, string> variables)
        {
            var template = await _templateRepository.GetTemplateByNameAsync(templateName);
            if (template == null || template.Channel != "SMS")
            {
                throw new ArgumentException($"SMS template not found: {templateName}");
            }

            return RenderTemplateContent(template.BodyTemplate, variables);
        }

        private string RenderTemplateContent(string template, Dictionary<string, string> variables)
        {
            if (string.IsNullOrEmpty(template) || variables == null)
                return template;

            foreach (var variable in variables)
            {
                template = template.Replace($"{{{{{variable.Key}}}}}", variable.Value);
            }

            return template;
        }
    }
}