using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeStay.ListingService.Models.Requests;
using WeStay.ListingService.Services.Interfaces;
using System.Security.Claims;
using WeStay.ListingService.Models.Requests;
using WeStay.ListingService.Models;
using WeStay.ListingService.Services.Interfaces;

namespace WeStay.ListingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(IBookingService bookingService, ILogger<BookingsController> logger)
        {
            _bookingService = bookingService;
            _logger = logger;
        }

        /// <summary>
        /// Get all bookings for the authenticated guest
        /// </summary>
        [HttpGet("my-bookings")]
        public async Task<IActionResult> GetMyBookings()
        {
            try
            {
                var guestId = GetUserId();
                var bookings = await _bookingService.GetBookingsByGuestIdAsync(guestId);

                return Ok(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookings for guest {GuestId}", GetUserId());
                return StatusCode(500, new { Message = "An error occurred while retrieving bookings" });
            }
        }

        /// <summary>
        /// Get all bookings for the authenticated host
        /// </summary>
        [HttpGet("host-bookings")]
        public async Task<IActionResult> GetHostBookings()
        {
            try
            {
                var hostId = GetUserId();
                var bookings = await _bookingService.GetBookingsByHostIdAsync(hostId);

                return Ok(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookings for host {HostId}", GetUserId());
                return StatusCode(500, new { Message = "An error occurred while retrieving host bookings" });
            }
        }

        /// <summary>
        /// Get a specific booking by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBooking(int id)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id);
                if (booking == null)
                {
                    return NotFound(new { Message = "Booking not found" });
                }

                // Check if user has access to this booking
                var userId = GetUserId();
                if (booking.GuestId != userId && booking.Listing.HostId != userId)
                {
                    return Forbid();
                }

                return Ok(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking {BookingId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving the booking" });
            }
        }

        /// <summary>
        /// Create a new booking
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid booking data", Errors = ModelState.Values.SelectMany(v => v.Errors) });
                }

                var guestId = GetUserId();
                var booking = await _bookingService.CreateBookingAsync(guestId, request);

                return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking for guest {GuestId}", GetUserId());
                return StatusCode(500, new { Message = "An error occurred while creating the booking" });
            }
        }

        /// <summary>
        /// Cancel a booking
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id, [FromBody] CancelBookingRequest request)
        {
            try
            {
                var userId = GetUserId();
                var booking = await _bookingService.UpdateBookingStatusAsync(id, userId, BookingStatus.Cancelled, request.Reason);

                return Ok(new { Message = "Booking cancelled successfully", Booking = booking });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking {BookingId} for user {UserId}", id, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while cancelling the booking" });
            }
        }

        /// <summary>
        /// Confirm a booking (host only)
        /// </summary>
        [HttpPost("{id}/confirm")]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            try
            {
                var userId = GetUserId();
                var booking = await _bookingService.UpdateBookingStatusAsync(id, userId, BookingStatus.Confirmed);

                return Ok(new { Message = "Booking confirmed successfully", Booking = booking });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming booking {BookingId} for user {UserId}", id, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while confirming the booking" });
            }
        }

        /// <summary>
        /// Reject a booking (host only)
        /// </summary>
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectBooking(int id, [FromBody] RejectBookingRequest request)
        {
            try
            {
                var userId = GetUserId();
                var booking = await _bookingService.UpdateBookingStatusAsync(id, userId, BookingStatus.Rejected, request.Reason);

                return Ok(new { Message = "Booking rejected successfully", Booking = booking });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting booking {BookingId} for user {UserId}", id, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while rejecting the booking" });
            }
        }

        /// <summary>
        /// Check if listing is available for dates
        /// </summary>
        [HttpGet("availability/{listingId}")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckAvailability(int listingId, [FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut)
        {
            try
            {
                var isAvailable = await _bookingService.IsListingAvailableAsync(listingId, checkIn, checkOut);
                return Ok(new { IsAvailable = isAvailable });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for listing {ListingId}", listingId);
                return StatusCode(500, new { Message = "An error occurred while checking availability" });
            }
        }

        /// <summary>
        /// Get unavailable dates for a listing
        /// </summary>
        [HttpGet("unavailable-dates/{listingId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUnavailableDates(int listingId, [FromQuery] int monthsAhead = 6)
        {
            try
            {
                var unavailableDates = await _bookingService.GetUnavailableDatesAsync(listingId, monthsAhead);
                return Ok(unavailableDates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unavailable dates for listing {ListingId}", listingId);
                return StatusCode(500, new { Message = "An error occurred while retrieving unavailable dates" });
            }
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }
    }

    public class CancelBookingRequest
    {
        public string Reason { get; set; }
    }

    public class RejectBookingRequest
    {
        public string Reason { get; set; }
    }
}