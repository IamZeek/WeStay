using WeStay.ListingService.Models;
using WeStay.ListingService.Models.Requests;
using WeStay.ListingService.Models.Requests;

namespace WeStay.ListingService.Services.Interfaces
{
    public interface ISearchService
    {
        Task<(IEnumerable<Listing> listings, int totalCount)> SearchListingsAsync(SearchListingsRequest request);
        Task<IEnumerable<Listing>> GetFeaturedListingsAsync();
        Task<IEnumerable<Listing>> GetSimilarListingsAsync(int listingId);
        Task<IEnumerable<string>> GetPopularSearchLocationsAsync(int count);
        Task<Dictionary<string, int>> GetListingStatsAsync();
    }
}