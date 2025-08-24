using WeStay.BookingService.Models;

namespace WeStay.BookingService.Repositories.Interfaces
{
    public interface IBookingReviewRepository
    {
        Task<BookingReview> GetReviewByIdAsync(int id);
        Task<BookingReview> GetReviewByBookingIdAsync(int bookingId);
        Task<IEnumerable<BookingReview>> GetReviewsByListingIdAsync(int listingId);
        Task<IEnumerable<BookingReview>> GetReviewsByUserIdAsync(int userId);
        Task<BookingReview> CreateReviewAsync(BookingReview review);
        Task<BookingReview> UpdateReviewAsync(BookingReview review);
        Task<bool> DeleteReviewAsync(int id);
    }
}