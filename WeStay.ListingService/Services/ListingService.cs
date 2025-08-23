﻿using Microsoft.EntityFrameworkCore;
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
    public class ListingService : IListingService
    {
        private readonly ListingDbContext _context;
        private readonly ILogger<ListingService> _logger;

        public ListingService(ListingDbContext context, ILogger<ListingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Listing> GetListingByIdAsync(int id)
        {
            return await _context.Listings
                .Include(l => l.Amenities)
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.Id == id && l.Status == ListingStatus.Active);
        }

        public async Task<IEnumerable<Listing>> GetListingsByHostIdAsync(int hostId)
        {
            return await _context.Listings
                .Include(l => l.Amenities)
                .Include(l => l.Images)
                .Where(l => l.HostId == hostId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<Listing> CreateListingAsync(int hostId, CreateListingRequest request)
        {
            var listing = new Listing
            {
                HostId = hostId,
                Title = request.Title,
                Description = request.Description,
                Type = request.Type,
                Guests = request.Guests,
                Bedrooms = request.Bedrooms,
                Beds = request.Beds,
                Bathrooms = request.Bathrooms,
                PricePerNight = request.PricePerNight,
                Address = request.Address,
                City = request.City,
                State = request.State,
                Country = request.Country,
                ZipCode = request.ZipCode,
                Status = ListingStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add amenities
            if (request.AmenityIds != null && request.AmenityIds.Any())
            {
                var amenities = await _context.Amenities
                    .Where(a => request.AmenityIds.Contains(a.Id))
                    .ToListAsync();
                listing.Amenities = amenities;
            }

            // Add images
            if (request.ImageUrls != null && request.ImageUrls.Any())
            {
                listing.Images = request.ImageUrls.Select((url, index) => new ListingImage
                {
                    ImageUrl = url,
                    IsPrimary = index == 0,
                    DisplayOrder = index,
                    CreatedAt = DateTime.UtcNow
                }).ToList();
            }

            _context.Listings.Add(listing);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created listing {ListingId} for host {HostId}", listing.Id, hostId);

            return await GetListingByIdAsync(listing.Id);
        }

        public async Task<Listing> UpdateListingAsync(int listingId, int hostId, UpdateListingRequest request)
        {
            var listing = await _context.Listings
                .Include(l => l.Amenities)
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.Id == listingId && l.HostId == hostId);

            if (listing == null)
            {
                throw new KeyNotFoundException("Listing not found or you don't have permission to update it");
            }

            // Update properties if provided
            if (!string.IsNullOrEmpty(request.Title)) listing.Title = request.Title;
            if (!string.IsNullOrEmpty(request.Description)) listing.Description = request.Description;
            if (request.Guests.HasValue) listing.Guests = request.Guests.Value;
            if (request.Bedrooms.HasValue) listing.Bedrooms = request.Bedrooms.Value;
            if (request.Beds.HasValue) listing.Beds = request.Beds.Value;
            if (request.Bathrooms.HasValue) listing.Bathrooms = request.Bathrooms.Value;
            if (request.PricePerNight.HasValue) listing.PricePerNight = request.PricePerNight.Value;
            if (!string.IsNullOrEmpty(request.Address)) listing.Address = request.Address;
            if (!string.IsNullOrEmpty(request.City)) listing.City = request.City;
            if (!string.IsNullOrEmpty(request.State)) listing.State = request.State;
            if (!string.IsNullOrEmpty(request.Country)) listing.Country = request.Country;
            if (!string.IsNullOrEmpty(request.ZipCode)) listing.ZipCode = request.ZipCode;

            listing.UpdatedAt = DateTime.UtcNow;

            // Update amenities if provided
            if (request.AmenityIds != null)
            {
                var amenities = await _context.Amenities
                    .Where(a => request.AmenityIds.Contains(a.Id))
                    .ToListAsync();
                listing.Amenities = amenities;
            }

            // Update images if provided
            if (request.ImageUrls != null)
            {
                // Remove existing images
                _context.ListingImages.RemoveRange(listing.Images);

                // Add new images
                listing.Images = request.ImageUrls.Select((url, index) => new ListingImage
                {
                    ImageUrl = url,
                    IsPrimary = index == 0,
                    DisplayOrder = index,
                    CreatedAt = DateTime.UtcNow
                }).ToList();
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated listing {ListingId} for host {HostId}", listingId, hostId);

            return await GetListingByIdAsync(listingId);
        }

        public async Task<bool> DeleteListingAsync(int listingId, int hostId)
        {
            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == listingId && l.HostId == hostId);

            if (listing == null)
            {
                return false;
            }

            // Soft delete by changing status
            listing.Status = ListingStatus.Inactive;
            listing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted listing {ListingId} for host {HostId}", listingId, hostId);

            return true;
        }

        public async Task<bool> ChangeListingStatusAsync(int listingId, int hostId, ListingStatus status)
        {
            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == listingId && l.HostId == hostId);

            if (listing == null)
            {
                return false;
            }

            listing.Status = status;
            listing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Changed status of listing {ListingId} to {Status} for host {HostId}",
                listingId, status, hostId);

            return true;
        }
    }
}