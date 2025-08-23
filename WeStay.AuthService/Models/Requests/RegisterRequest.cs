using System.ComponentModel.DataAnnotations;

namespace WeStay.AuthService.Models.Requests
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }
    }
}