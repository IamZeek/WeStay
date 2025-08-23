using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    public class SearchService : ISearchService
    {
        private readonly ListingDbContext _context;
        private readonly ILogger<SearchService> _logger;

        public SearchService(ListingDbContext context, ILogger<SearchService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(IEnumerable<Listing> listings, int totalCount)> SearchListingsAsync(SearchListingsRequest request)
        {
            var query = _context.Listings
                .Include(l => l.Amenities)
                .Include(l => l.Images)
                .Where(l => l.Status == ListingStatus.Active)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.Location))
            {
                query = query.Where(l =>
                    l.City.Contains(request.Location) ||
                    l.State.Contains(request.Location) ||
                    l.Country.Contains(request.Location) ||
                    l.Address.Contains(request.Location));
            }

            if (request.Guests.HasValue)
            {
                query = query.Where(l => l.Guests >= request.Guests.Value);
            }

            if (request.Bedrooms.HasValue)
            {
                query = query.Where(l => l.Bedrooms >= request.Bedrooms.Value);
            }

            if (request.Beds.HasValue)
            {
                query = query.Where(l => l.Beds >= request.Beds.Value);
            }

            if (request.Bathrooms.HasValue)
            {
                query = query.Where(l => l.Bathrooms >= request.Bathrooms.Value);
            }

            if (request.MinPrice.HasValue)
            {
                query = query.Where(l => l.PricePerNight >= request.MinPrice.Value);
            }

            if (request.MaxPrice.HasValue)
            {
                query = query.Where(l => l.PricePerNight <= request.MaxPrice.Value);
            }

            if (request.Type.HasValue)
            {
                query = query.Where(l => l.Type == request.Type.Value);
            }

            if (request.AmenityIds != null && request.AmenityIds.Any())
            {
                query = query.Where(l => l.Amenities.Any(a => request.AmenityIds.Contains(a.Id)));
            }

            // Date availability check
            if (request.CheckInDate.HasValue && request.CheckOutDate.HasValue)
            {
                query = query.Where(l => !l.Bookings.Any(b =>
                    b.Status != BookingStatus.Cancelled &&
                    b.Status != BookingStatus.Rejected &&
                    ((b.CheckInDate <= request.CheckInDate && b.CheckOutDate > request.CheckInDate) ||
                     (b.CheckInDate < request.CheckOutDate && b.CheckOutDate >= request.CheckOutDate) ||
                     (b.CheckInDate >= request.CheckInDate && b.CheckOutDate <= request.CheckOutDate))));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = request.SortBy.ToLower() switch
            {
                "price" => request.SortDescending ? query.OrderByDescending(l => l.PricePerNight) : query.OrderBy(l => l.PricePerNight),
                "rating" => query.OrderByDescending(l => 0), // Placeholder for when ratings are implemented
                "createdat" => request.SortDescending ? query.OrderByDescending(l => l.CreatedAt) : query.OrderBy(l => l.CreatedAt),
                _ => request.SortDescending ? query.OrderByDescending(l => l.CreatedAt) : query.OrderBy(l => l.CreatedAt)
            };

            // Apply pagination
            query = query.Skip((request.Page - 1) * request.PageSize)
                        .Take(request.PageSize);

            var listings = await query.ToListAsync();

            _logger.LogInformation("Search returned {Count} listings out of {TotalCount} for query: {Query}",
                listings.Count, totalCount, request);

            return (listings, totalCount);
        }

        public async Task<IEnumerable<Listing>> GetFeaturedListingsAsync()
        {
            // Get random active listings with images
            var featuredListings = await _context.Listings
                .Include(l => l.Amenities)
                .Include(l => l.Images)
                .Where(l => l.Status == ListingStatus.Active && l.Images.Any())
                .OrderBy(l => Guid.NewGuid()) // Random order
                .Take(8)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} featured listings", featuredListings.Count);

            return featuredListings;
        }

        public async Task<IEnumerable<Listing>> GetSimilarListingsAsync(int listingId)
        {
            var currentListing = await _context.Listings
                .Include(l => l.Amenities)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (currentListing == null)
            {
                return new List<Listing>();
            }

            // Find similar listings based on type, location, and amenities
            var similarListings = await _context.Listings
                .Include(l => l.Amenities)
                .Include(l => l.Images)
                .Where(l => l.Id != listingId &&
                           l.Status == ListingStatus.Active &&
                           (l.Type == currentListing.Type ||
                            l.City == currentListing.City ||
                            l.Country == currentListing.Country ||
                            l.Amenities.Any(a => currentListing.Amenities.Select(a => a.Id).Contains(a.Id))))
                .OrderBy(l => Guid.NewGuid()) // Random order
                .Take(6)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} similar listings for listing {ListingId}",
                similarListings.Count, listingId.ToString());

            return similarListings;
        }

        public async Task<IEnumerable<string>> GetPopularSearchLocationsAsync(int count = 10)
        {
            var popularLocations = await _context.Listings
                .Where(l => l.Status == ListingStatus.Active)
                .GroupBy(l => l.City)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(count)
                .ToListAsync();

            return popularLocations.Where(l => !string.IsNullOrEmpty(l));
        }

        public async Task<Dictionary<string, int>> GetListingStatsAsync()
        {
            var stats = new Dictionary<string, int>
            {
                ["totalListings"] = await _context.Listings.CountAsync(l => l.Status == ListingStatus.Active),
                ["totalBookings"] = await _context.Bookings.CountAsync(b =>
                    b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed),
                ["activeHosts"] = await _context.Listings
                    .Where(l => l.Status == ListingStatus.Active)
                    .Select(l => l.HostId)
                    .Distinct()
                    .CountAsync()
            };

            return stats;
        }
    }
}