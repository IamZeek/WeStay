namespace WeStay.NotificationService.Services.Interfaces
{
    public interface ITemplateService
    {
        Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> variables);
        Task<(string Subject, string Body)> RenderEmailTemplateAsync(string templateName, Dictionary<string, string> variables);
        Task<string> RenderSMSTemplateAsync(string templateName, Dictionary<string, string> variables);
    }
}