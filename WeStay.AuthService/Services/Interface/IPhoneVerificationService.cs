namespace WeStay.AuthService.Services.Interfaces
{
    public interface IPhoneVerificationService
    {
        Task SendOtpAsync(string phoneNumber);
        Task<bool> VerifyOtp(string phoneNumber, string otp, int userId);
    }
}
