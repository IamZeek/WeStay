using Microsoft.EntityFrameworkCore;
using WeStay.ListingService.Data;
using WeStay.ListingService.Models;
using WeStay.ListingService.Models.Requests;
using WeStay.ListingService.Services.Interfaces;
using WeStay.ListingService.Data;
using WeStay.ListingService.Models.Requests;
using WeStay.ListingService.Models;
using WeStay.ListingService.Services.Interfaces;

namespace WeStay.ListingService.Services
{
    public class BookingService : IBookingService
    {
        private readonly ListingDbContext _context;
        private readonly ILogger<BookingService> _logger;

        public BookingService(ListingDbContext context, ILogger<BookingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Booking> GetBookingByIdAsync(int id)
        {
            return await _context.Bookings
                .Include(b => b.Listing)
                .ThenInclude(l => l.Images)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<IEnumerable<Booking>> GetBookingsByGuestIdAsync(int guestId)
        {
            return await _context.Bookings
                .Include(b => b.Listing)
                .ThenInclude(l => l.Images)
                .Where(b => b.GuestId == guestId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetBookingsByHostIdAsync(int hostId)
        {
            return await _context.Bookings
                .Include(b => b.Listing)
                .ThenInclude(l => l.Images)
                .Include(b => b.Listing)
                .ThenInclude(l => l.Amenities)
                .Where(b => b.Listing.HostId == hostId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<Booking> CreateBookingAsync(int guestId, CreateBookingRequest request)
        {
            // Check if listing exists and is active
            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == request.ListingId && l.Status == ListingStatus.Active);

            if (listing == null)
            {
                throw new KeyNotFoundException("Listing not found or not available");
            }

            // Check if listing is available for the requested dates
            if (!await IsListingAvailableAsync(request.ListingId, request.CheckInDate, request.CheckOutDate))
            {
                throw new InvalidOperationException("Listing is not available for the selected dates");
            }

            // Check if guests count is within listing capacity
            if (request.Guests > listing.Guests)
            {
                throw new InvalidOperationException($"Listing can only accommodate {listing.Guests} guests");
            }

            // Calculate total price
            var nights = (request.CheckOutDate - request.CheckInDate).Days;
            var totalPrice = nights * listing.PricePerNight;

            var booking = new Booking
            {
                ListingId = request.ListingId,
                GuestId = guestId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                Guests = request.Guests,
                TotalPrice = totalPrice,
                SpecialRequests = request.SpecialRequests,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created booking {BookingId} for guest {GuestId} on listing {ListingId}",
                booking.Id, guestId, request.ListingId);

            return await GetBookingByIdAsync(booking.Id);
        }

        public async Task<Booking> UpdateBookingStatusAsync(int bookingId, int userId, BookingStatus status, string reason = null)
        {
            var booking = await _context.Bookings
                .Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                throw new KeyNotFoundException("Booking not found");
            }

            // Check if user has permission to update this booking
            // User can be either the guest (for cancellations) or the host (for confirmations/rejections)
            var isGuest = booking.GuestId == userId;
            var isHost = booking.Listing.HostId == userId;

            if (!isGuest && !isHost)
            {
                throw new UnauthorizedAccessException("You don't have permission to update this booking");
            }

            // Validate status transitions
            if (isGuest && status != BookingStatus.Cancelled)
            {
                throw new InvalidOperationException("Guests can only cancel bookings");
            }

            if (isHost && (status != BookingStatus.Confirmed && status != BookingStatus.Rejected))
            {
                throw new InvalidOperationException("Hosts can only confirm or reject bookings");
            }

            // Update booking status
            booking.Status = status;
            booking.UpdatedAt = DateTime.UtcNow;

            if (status == BookingStatus.Cancelled || status == BookingStatus.Rejected)
            {
                booking.CancelledAt = DateTime.UtcNow;
                booking.CancellationReason = reason;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated booking {BookingId} status to {Status} by user {UserId}",
                bookingId, status, userId);

            return await GetBookingByIdAsync(bookingId);
        }

        public async Task<bool> IsListingAvailableAsync(int listingId, DateTime checkIn, DateTime checkOut)
        {
            // Check if there are any conflicting bookings
            var conflictingBookings = await _context.Bookings
                .Where(b => b.ListingId == listingId &&
                           b.Status != BookingStatus.Cancelled &&
                           b.Status != BookingStatus.Rejected &&
                           ((b.CheckInDate <= checkIn && b.CheckOutDate > checkIn) ||
                            (b.CheckInDate < checkOut && b.CheckOutDate >= checkOut) ||
                            (b.CheckInDate >= checkIn && b.CheckOutDate <= checkOut)))
                .AnyAsync();

            return !conflictingBookings;
        }

        public async Task<IEnumerable<DateTime>> GetUnavailableDatesAsync(int listingId, int monthsAhead = 6)
        {
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddMonths(monthsAhead);

            var bookings = await _context.Bookings
                .Where(b => b.ListingId == listingId &&
                           b.Status != BookingStatus.Cancelled &&
                           b.Status != BookingStatus.Rejected &&
                           b.CheckInDate <= endDate &&
                           b.CheckOutDate >= startDate)
                .ToListAsync();

            var unavailableDates = new List<DateTime>();
            foreach (var booking in bookings)
            {
                for (var date = booking.CheckInDate; date < booking.CheckOutDate; date = date.AddDays(1))
                {
                    if (date >= startDate && date <= endDate)
                    {
                        unavailableDates.Add(date);
                    }
                }
            }

            return unavailableDates.Distinct().OrderBy(d => d);
        }
    }
}