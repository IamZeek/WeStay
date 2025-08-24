using System.ComponentModel.DataAnnotations;

namespace WeStay.BookingService.DTOs
{
    public class AvailabilityCheckRequest
    {
        [Required]
        public int ListingId { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Range(1, 20)]
        public int NumberOfGuests { get; set; } = 1;
    }
}