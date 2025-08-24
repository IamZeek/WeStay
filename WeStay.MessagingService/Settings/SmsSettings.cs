namespace WeStay.MessagingService.Settings
{
    public class SmsSettings
    {
        public string TwilioAccountSid { get; set; } = string.Empty;
        public string TwilioAuthToken { get; set; } = string.Empty;
        public string TwilioPhoneNumber { get; set; } = string.Empty;
        public bool Enabled { get; set; }
    }
}