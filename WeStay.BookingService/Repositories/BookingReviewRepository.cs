using Microsoft.EntityFrameworkCore;
using WeStay.BookingService.Data;
using WeStay.BookingService.Models;
using WeStay.BookingService.Repositories.Interfaces;

namespace WeStay.BookingService.Repositories
{
    public class BookingReviewRepository : IBookingReviewRepository
    {
        private readonly BookingDbContext _context;
        private readonly ILogger<BookingReviewRepository> _logger;

        public BookingReviewRepository(BookingDbContext context, ILogger<BookingReviewRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BookingReview> GetReviewByIdAsync(int id)
        {
            return await _context.BookingReviews
                .Include(r => r.Booking)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<BookingReview> GetReviewByBookingIdAsync(int bookingId)
        {
            return await _context.BookingReviews
                .Include(r => r.Booking)
                .FirstOrDefaultAsync(r => r.BookingId == bookingId);
        }

        public async Task<IEnumerable<BookingReview>> GetReviewsByListingIdAsync(int listingId)
        {
            return await _context.BookingReviews
                .Include(r => r.Booking)
                .Where(r => r.Booking.ListingId == listingId && r.IsPublished)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookingReview>> GetReviewsByUserIdAsync(int userId)
        {
            return await _context.BookingReviews
                .Include(r => r.Booking)
                .Where(r => r.Booking.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<BookingReview> CreateReviewAsync(BookingReview review)
        {
            review.CreatedAt = DateTime.UtcNow;
            review.UpdatedAt = DateTime.UtcNow;

            _context.BookingReviews.Add(review);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created review {ReviewId} for booking {BookingId}", review.Id, review.BookingId);

            return review;
        }

        public async Task<BookingReview> UpdateReviewAsync(BookingReview review)
        {
            review.UpdatedAt = DateTime.UtcNow;
            _context.BookingReviews.Update(review);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated review {ReviewId}", review.Id);

            return review;
        }

        public async Task<bool> DeleteReviewAsync(int id)
        {
            var review = await GetReviewByIdAsync(id);
            if (review == null)
            {
                return false;
            }

            _context.BookingReviews.Remove(review);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted review {ReviewId}", id);

            return true;
        }

        public async Task<double> GetAverageRatingForListingAsync(int listingId)
        {
            var averageRating = await _context.BookingReviews
                .Include(r => r.Booking)
                .Where(r => r.Booking.ListingId == listingId && r.IsPublished)
                .AverageAsync(r => (double?)r.Rating) ?? 0;

            return Math.Round(averageRating, 1);
        }

        public async Task<int> GetReviewCountForListingAsync(int listingId)
        {
            return await _context.BookingReviews
                .Include(r => r.Booking)
                .Where(r => r.Booking.ListingId == listingId && r.IsPublished)
                .CountAsync();
        }
    }
}