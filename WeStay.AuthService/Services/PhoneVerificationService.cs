using Twilio;
using Twilio.Rest.Api.V2010.Account;
using WeStay.AuthService.Services.Interfaces;

namespace WeStay.AuthService.Services
{
    public class PhoneVerificationService : IPhoneVerificationService
    {
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, string> _otpStorage = new();
        private readonly IUserService _userService;

        public PhoneVerificationService(IConfiguration configuration, IUserService userService)
        {
            _configuration = configuration;

            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];
            TwilioClient.Init(accountSid, authToken);
            _userService = userService;
        }

        public async Task SendOtpAsync(string phoneNumber)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            _otpStorage[phoneNumber] = otp;

            await MessageResource.CreateAsync(
                body: $"Your verification code is: {otp}",
                from: new Twilio.Types.PhoneNumber(_configuration["Twilio:FromPhone"]),
                to: new Twilio.Types.PhoneNumber(phoneNumber)
            );
        }

        public async Task<bool> VerifyOtp(string phoneNumber, string otp,int userId)
        {
            bool otpVerification = _otpStorage.ContainsKey(phoneNumber) && _otpStorage[phoneNumber] == otp;
            if (otpVerification)
                return await _userService.UpdateUserStatusAsync(userId,"Phone");  //Update Status in database
            return false;
        }
    }
}
