using WeStay.AuthService.Models;

namespace WeStay.AuthService.Services.Interfaces
{
    public interface IVerificationService
    {
        Task<Verification> GetVerificationByUserIdAsync(int userId);
        Task<Verification> GetVerificationByIdAsync(int id);
        Task<Verification> CreateVerificationAsync(Verification verification);
        Task<Verification> UpdateVerificationAsync(Verification verification);
        Task<Verification> UpdateVerificationStatusAsync(int verificationId, VerificationStatus status, string rejectionReason = null);

        Task<bool> DeleteVerificationAsync(int id);
        Task<IEnumerable<Verification>> GetVerificationsByStatusAsync(VerificationStatus status);
        Task<bool> UserHasVerificationAsync(int userId);
    }
}