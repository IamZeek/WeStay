using WeStay.AuthService.Models;

namespace WeStay.AuthService.Services.Interfaces
{
    public interface IAuthService
    {
        Task<User> RegisterUserAsync(string email, string password, string firstName, string lastName, DateTime? dateOfBirth, string phoneNumber);
        Task<User> LoginUserAsync(string email, string password);
        Task<User> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

    }
}