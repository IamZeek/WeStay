using Microsoft.EntityFrameworkCore;
using WeStay.BookingService.Data;
using WeStay.BookingService.Models;
using WeStay.BookingService.Repositories.Interfaces;

namespace WeStay.BookingService.Repositories
{
    public class BookingStatusRepository : IBookingStatusRepository
    {
        private readonly BookingDbContext _context;

        public BookingStatusRepository(BookingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BookingStatus>> GetAllStatusesAsync()
        {
            return await _context.BookingStatuses.ToListAsync();
        }

        public async Task<BookingStatus> GetStatusByIdAsync(int id)
        {
            return await _context.BookingStatuses.FindAsync(id);
        }

        public async Task<BookingStatus> GetStatusByNameAsync(string name)
        {
            return await _context.BookingStatuses
                .FirstOrDefaultAsync(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}