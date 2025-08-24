namespace WeStay.MessagingService.Models
{
    public class BroadcastMessage
    {
        public string Channel { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}