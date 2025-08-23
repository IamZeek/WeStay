using System.ComponentModel.DataAnnotations;

namespace WeStay.AuthService.Models
{
    public class ExternalLogin
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public User User { get; set; }

        [Required]
        public string Provider { get; set; }

        [Required]
        public string ProviderKey { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}