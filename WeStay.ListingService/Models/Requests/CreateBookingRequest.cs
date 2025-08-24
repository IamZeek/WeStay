using System.ComponentModel.DataAnnotations;

namespace WeStay.ListingService.Models.Requests
{
    public class CreateBookingRequest
    {
        [Required]
        public int ListingId { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Required]
        [Range(1, 50)]
        public int Guests { get; set; }

        [MaxLength(500)]
        public string SpecialRequests { get; set; }
    }
}