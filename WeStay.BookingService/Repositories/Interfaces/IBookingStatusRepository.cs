using WeStay.BookingService.Models;

namespace WeStay.BookingService.Repositories.Interfaces
{
    public interface IBookingStatusRepository
    {
        Task<IEnumerable<BookingStatus>> GetAllStatusesAsync();
        Task<BookingStatus> GetStatusByIdAsync(int id);
        Task<BookingStatus> GetStatusByNameAsync(string name);
    }
}