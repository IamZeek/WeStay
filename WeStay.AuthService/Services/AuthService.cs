using WeStay.AuthService.Models;
using WeStay.AuthService.Services.Interfaces;

namespace WeStay.AuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthService> _logger;
        private readonly IVerificationService _verificationService;

        public AuthService(IUserService userService, ILogger<AuthService> logger, IVerificationService verificationService)
        {
            _userService = userService;
            _logger = logger;
            _verificationService = verificationService;
        }

        public async Task<User> RegisterUserAsync(string email, string password, string firstName, string lastName, DateTime? dateOfBirth, string phoneNumber)
        {
            // Check if user already exists
            if (await _userService.UserExistsAsync(email))
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            // Create new user
            var user = new User
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                PhoneNumber = phoneNumber,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Hash the password using BCrypt
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // Add user to database
            var createdUser = await _userService.CreateUserAsync(user);

            // Create empty verification record
            await CreateEmptyVerificationRecord(createdUser.Id);

            return createdUser;
        }

        private async Task CreateEmptyVerificationRecord(int userId)
        {
            try
            {
                var verification = new Verification
                {
                    UserId = userId,
                    DocumentType = DocumentType.Other, // Default value
                    DocumentNumber = "PENDING", // Placeholder value
                    ImageUrl = "", // Empty string
                    Status = VerificationStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _verificationService.CreateVerificationAsync(verification);
                _logger.LogInformation("Created empty verification record for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the registration
                _logger.LogError(ex, "Failed to create empty verification record for user {UserId}", userId);
            }
        }

        public async Task<User> LoginUserAsync(string email, string password)
        {
            // Find user by email
            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // Verify password
            if (!await _userService.CheckPasswordAsync(user, password))
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // Check if account is active
            if (!user.IsActive)
            {
                throw new InvalidOperationException("Account is deactivated");
            }

            // Update last login time
            user.UpdatedAt = DateTime.UtcNow;
            return await _userService.UpdateUserAsync(user);
        }

        public async Task<User> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            // Get user
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            // Verify current password
            if (!await _userService.CheckPasswordAsync(user, currentPassword))
            {
                throw new UnauthorizedAccessException("Current password is incorrect");
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            return await _userService.UpdateUserAsync(user);
        }
    }
}