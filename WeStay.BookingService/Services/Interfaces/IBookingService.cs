using WeStay.BookingService.Models;

namespace WeStay.BookingService.Services.Interfaces
{
    public interface IBookingService
    {
        Task<Booking> CreateBookingAsync(Booking booking, List<BookingGuest> guests);
        Task<Booking> GetBookingByIdAsync(int id);
        Task<Booking> GetBookingByCodeAsync(string bookingCode);
        Task<IEnumerable<Booking>> GetUserBookingsAsync(int userId);
        Task<IEnumerable<Booking>> GetListingBookingsAsync(int listingId);
        Task<Booking> CancelBookingAsync(int bookingId, string reason);
        Task<Booking> ConfirmBookingAsync(int bookingId);
        Task<bool> IsListingAvailableAsync(int listingId, DateTime checkInDate, DateTime checkOutDate, int? excludeBookingId = null);
        Task<decimal> CalculateBookingPriceAsync(int listingId, DateTime checkInDate, DateTime checkOutDate, int numberOfGuests);
    }
}