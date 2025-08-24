﻿using WeStay.BookingService.Repositories.Interfaces;
using WeStay.BookingService.Services.Interfaces;

namespace WeStay.BookingService.Services
{
    public class AvailabilityService : IAvailabilityService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ILogger<AvailabilityService> _logger;

        public AvailabilityService(IBookingRepository bookingRepository, ILogger<AvailabilityService> logger)
        {
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        public async Task<bool> IsListingAvailableAsync(int listingId, DateTime checkInDate, DateTime checkOutDate, int? excludeBookingId = null)
        {
            if (checkInDate >= checkOutDate)
            {
                throw new ArgumentException("Check-in date must be before check-out date");
            }

            if (checkInDate < DateTime.Today)
            {
                throw new ArgumentException("Check-in date cannot be in the past");
            }

            return await _bookingRepository.IsListingAvailableAsync(listingId, checkInDate, checkOutDate, excludeBookingId);
        }

        public async Task<IEnumerable<DateTime>> GetUnavailableDatesAsync(int listingId, DateTime startDate, DateTime endDate)
        {
            var unavailableDates = new List<DateTime>();
            var bookings = await _bookingRepository.GetBookingsByListingIdAsync(listingId);

            foreach (var booking in bookings.Where(b => b.StatusId != 3 && b.StatusId != 5)) // Not cancelled or refunded
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