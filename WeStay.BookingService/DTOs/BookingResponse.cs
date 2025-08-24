﻿namespace WeStay.BookingService.DTOs
{
    public class BookingResponse
    {
        public int Id { get; set; }
        public string BookingCode { get; set; }
        public int ListingId { get; set; }
        public int UserId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string SpecialRequests { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<GuestResponse> Guests { get; set; }
        public PaymentInfoResponse PaymentInfo { get; set; }
    }

    public class GuestResponse
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class PaymentInfoResponse
    {
        public string PaymentStatus { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}