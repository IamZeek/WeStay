namespace WeStay.BookingService.Services.Interfaces
{
    public interface IAvailabilityService
    {
        Task<bool> IsListingAvailableAsync(int listingId, DateTime checkInDate, DateTime checkOutDate, int? excludeBookingId = null);
        Task<IEnumerable<DateTime>> GetUnavailableDatesAsync(int listingId, DateTime startDate, DateTime endDate);
    }
}