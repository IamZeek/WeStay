using Microsoft.EntityFrameworkCore;
using WeStay.BookingService.Data;
using WeStay.BookingService.Models;
using WeStay.BookingService.Repositories.Interfaces;

namespace WeStay.BookingService.Repositories
{
    public class BookingPaymentRepository : IBookingPaymentRepository
    {
        private readonly BookingDbContext _context;
        private readonly ILogger<BookingPaymentRepository> _logger;

        public BookingPaymentRepository(BookingDbContext context, ILogger<BookingPaymentRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BookingPayment> GetPaymentByIdAsync(int id)
        {
            return await _context.BookingPayments.FindAsync(id);
        }

        public async Task<BookingPayment> GetPaymentByIntentIdAsync(string paymentIntentId)
        {
            return await _context.BookingPayments
                .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId);
        }

        public async Task<IEnumerable<BookingPayment>> GetPaymentsByBookingIdAsync(int bookingId)
        {
            return await _context.BookingPayments
                .Where(p => p.BookingId == bookingId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<BookingPayment> CreatePaymentAsync(BookingPayment payment)
        {
            _context.BookingPayments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<BookingPayment> UpdatePaymentAsync(BookingPayment payment)
        {
            payment.UpdatedAt = DateTime.UtcNow;
            _context.BookingPayments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<bool> UpdatePaymentStatusAsync(string paymentIntentId, string status, DateTime? paidAt = null, decimal? refundAmount = null)
        {
            var payment = await GetPaymentByIntentIdAsync(paymentIntentId);
            if (payment == null) return false;

            payment.PaymentStatus = status;
            payment.UpdatedAt = DateTime.UtcNow;

            if (status == "succeeded" && paidAt.HasValue)
            {
                payment.PaidAt = paidAt.Value;
            }

            if (status == "refunded" && refundAmount.HasValue)
            {
                payment.RefundAmount = refundAmount.Value;
                payment.RefundedAt = DateTime.UtcNow;
            }

            _context.BookingPayments.Update(payment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}