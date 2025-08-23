using System.ComponentModel.DataAnnotations;

namespace WeStay.AuthService.Models.Requests
{
    public class FacebookSignInRequest
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string ProfilePicture { get; set; }
    }
}