using System.ComponentModel.DataAnnotations;

namespace WeStay.AuthService.Models.Requests
{
    public class GoogleSignInRequest
    {
        [Required]
        public string IdToken { get; set; }
    }
}