using WeStay.ListingService.Models;
using WeStay.ListingService.Models.Requests;
using WeStay.ListingService.Models.Requests;
using WeStay.ListingService.Models;

namespace WeStay.ListingService.Services.Interfaces
{
    public interface IBookingService
    {
        Task<Booking> GetBookingByIdAsync(int id);
        Task<IEnumerable<DateTime>> GetUnavailableDatesAsync(int listingId, int monthsAhead);
        Task<IEnumerable<Booking>> GetBookingsByGuestIdAsync(int guestId);
        Task<IEnumerable<Booking>> GetBookingsByHostIdAsync(int hostId);
        Task<Booking> CreateBookingAsync(int guestId, CreateBookingRequest request);
        Task<Booking> UpdateBookingStatusAsync(int bookingId, int userId, BookingStatus status, string reason = null);
        Task<bool> IsListingAvailableAsync(int listingId, DateTime checkIn, DateTime checkOut);
    }
}