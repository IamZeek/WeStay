namespace WeStay.NotificationService.Models
{
    public class TwilioSettings
    {
        public string AccountSid { get; set; }
        public string AuthToken { get; set; }
        public string FromNumber { get; set; }
        public bool TestMode { get; set; }
        public string TestNumber { get; set; }
    }
}