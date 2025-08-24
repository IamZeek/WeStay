namespace WeStay.NotificationService.Models
{
    public class PushNotificationSettings
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public string Subject { get; set; }
        public string FirebaseProjectId { get; set; }
        public string FirebasePrivateKey { get; set; }
        public string FirebaseClientEmail { get; set; }
    }
}