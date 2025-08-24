using System.ComponentModel.DataAnnotations;

namespace WeStay.BookingService.DTOs
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
        [Range(1, 20)]
        public int NumberOfGuests { get; set; }

        public string SpecialRequests { get; set; }

        [Required]
        public List<GuestInfo> Guests { get; set; }
    }

    public class GuestInfo
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; }

        [Phone]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }
    }
}