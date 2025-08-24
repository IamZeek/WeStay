using WeStay.BookingService.Models;

namespace WeStay.BookingService.Repositories.Interfaces
{
    public interface IBookingPaymentRepository
    {
        Task<BookingPayment> GetPaymentByIdAsync(int id);
        Task<BookingPayment> GetPaymentByIntentIdAsync(string paymentIntentId);
        Task<IEnumerable<BookingPayment>> GetPaymentsByBookingIdAsync(int bookingId);
        Task<BookingPayment> CreatePaymentAsync(BookingPayment payment);
        Task<BookingPayment> UpdatePaymentAsync(BookingPayment payment);
        Task<bool> UpdatePaymentStatusAsync(string paymentIntentId, string status, DateTime? paidAt = null, decimal? refundAmount = null);
    }
}