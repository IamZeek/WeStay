﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeStay.BookingService.DTOs;
using WeStay.BookingService.Models;
using WeStay.BookingService.Services.Interfaces;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using WeStay.BookingService.Services;

namespace WeStay.BookingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IAvailabilityService _availabilityService;
        private readonly ILogger<BookingsController> _logger;
        public BookingsController(
            IBookingService bookingService,
            IAvailabilityService availabilityService,
            ILogger<BookingsController> logger)
        {
            _bookingService = bookingService;
            _availabilityService = availabilityService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid booking data", Errors = ModelState.Values.SelectMany(v => v.Errors) });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var booking = new Booking
                {
                    ListingId = request.ListingId,
                    UserId = userId,
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate,
                    NumberOfGuests = request.NumberOfGuests,
                    SpecialRequests = request.SpecialRequests
                };

                var guests = request.Guests.Select(g => new BookingGuest
                {
                    FirstName = g.FirstName,
                    LastName = g.LastName,
                    Email = g.Email,
                    PhoneNumber = g.PhoneNumber,
                    DateOfBirth = g.DateOfBirth
                }).ToList();

                var createdBooking = await _bookingService.CreateBookingAsync(booking, guests);

                return Ok(new
                {
                    Message = "Booking created successfully",
                    Booking = new
                    {
                        createdBooking.Id,
                        createdBooking.BookingCode,
                        createdBooking.TotalPrice,
                        createdBooking.Currency,
                        Status = "Pending"
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { Message = "An error occurred while creating booking" });
            }
        }

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

                // Check if user owns the booking or is admin
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (booking.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                return Ok(MapToBookingResponse(booking));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving booking {BookingId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving booking" });
            }
        }

        [HttpGet("code/{bookingCode}")]
        public async Task<IActionResult> GetBookingByCode(string bookingCode)
        {
            try
            {
                var booking = await _bookingService.GetBookingByCodeAsync(bookingCode);
                if (booking == null)
                {
                    return NotFound(new { Message = "Booking not found" });
                }

                // Check if user owns the booking or is admin
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (booking.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                return Ok(MapToBookingResponse(booking));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving booking with code {BookingCode}", bookingCode);
                return StatusCode(500, new { Message = "An error occurred while retrieving booking" });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserBookings(int userId)
        {
            try
            {
                // Check if user is accessing their own bookings or is admin
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (userId != currentUserId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                var bookings = await _bookingService.GetUserBookingsAsync(userId);
                return Ok(bookings.Select(MapToBookingResponse));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bookings for user {UserId}", userId);
                return StatusCode(500, new { Message = "An error occurred while retrieving bookings" });
            }
        }

        [HttpPost("availability")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckAvailability([FromBody] AvailabilityCheckRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid availability check data", Errors = ModelState.Values.SelectMany(v => v.Errors) });
                }

                var isAvailable = await _availabilityService.IsListingAvailableAsync(
                    request.ListingId, request.CheckInDate, request.CheckOutDate);

                var price = await _bookingService.CalculateBookingPriceAsync(
                    request.ListingId, request.CheckInDate, request.CheckOutDate, request.NumberOfGuests);

                return Ok(new
                {
                    IsAvailable = isAvailable,
                    TotalPrice = price,
                    Nights = (request.CheckOutDate - request.CheckInDate).Days,
                    Currency = "USD"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for listing {ListingId}", request.ListingId);
                return StatusCode(500, new { Message = "An error occurred while checking availability" });
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id, [FromBody] CancelBookingRequest request)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id);
                if (booking == null)
                {
                    return NotFound(new { Message = "Booking not found" });
                }

                // Check if user owns the booking or is admin
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (booking.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                var cancelledBooking = await _bookingService.CancelBookingAsync(id, request.Reason);

                return Ok(new
                {
                    Message = "Booking cancelled successfully",
                    Booking = MapToBookingResponse(cancelledBooking)
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking {BookingId}", id);
                return StatusCode(500, new { Message = "An error occurred while cancelling booking" });
            }
        }

        private BookingResponse MapToBookingResponse(Booking booking)
        {
            return new BookingResponse
            {
                Id = booking.Id,
                BookingCode = booking.BookingCode,
                ListingId = booking.ListingId,
                UserId = booking.UserId,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                NumberOfGuests = booking.NumberOfGuests,
                TotalPrice = booking.TotalPrice,
                Currency = booking.Currency,
                Status = booking.Status?.Name,
                SpecialRequests = booking.SpecialRequests,
                CreatedAt = booking.CreatedAt,
                Guests = booking.Guests?.Select(g => new GuestResponse
                {
                    FirstName = g.FirstName,
                    LastName = g.LastName,
                    Email = g.Email,
                    PhoneNumber = g.PhoneNumber
                }).ToList(),
                PaymentInfo = booking.Payments?.OrderByDescending(p => p.CreatedAt)
                    .Select(p => new PaymentInfoResponse
                    {
                        PaymentStatus = p.PaymentStatus,
                        Amount = p.Amount,
                        PaidAt = p.PaidAt
                    }).FirstOrDefault()
            };
        }
    }

    public class CancelBookingRequest
    {
        [Required]
        public string Reason { get; set; }
    }
}