using Microsoft.EntityFrameworkCore;
using WeStay.AuthService.Data;
using WeStay.AuthService.Models;
using WeStay.AuthService.Services.Interfaces;
using System.Security.Claims;

namespace WeStay.AuthService.Services
{
    public class ExternalAuthService : IExternalAuthService
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<ExternalAuthService> _logger;
        private readonly IVerificationService _verificationService;


        public ExternalAuthService(AuthDbContext context, ILogger<ExternalAuthService> logger, IVerificationService verificationService)
        {
            _context = context;
            _logger = logger;
            _verificationService = verificationService; // Add this line
        }

        public async Task<User> HandleExternalLoginAsync(string provider, IEnumerable<Claim> claims)
        {
            var externalId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(externalId))
            {
                _logger.LogWarning("External authentication provider {Provider} did not provide a NameIdentifier claim", provider);
                return null;
            }

            // Try to find existing user
            var user = await FindUserByExternalIdAsync(provider, externalId);
            if (user != null)
            {
                _logger.LogInformation("Found existing user {UserId} for external login {Provider}:{ExternalId}",
                    user.Id, provider, externalId);
                return user;
            }

            // Try to find by email
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
                    // Link external account to existing user
                    user.ExternalId = externalId;
                    user.ExternalType = provider;
                    user.ExternalSubject = claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                    user.ExternalIssuer = claims.FirstOrDefault(c => c.Type == "iss")?.Value;
                    user.UpdatedAt = DateTime.UtcNow;

                    // Add to external logins history
                    _context.ExternalLogins.Add(new ExternalLogin
                    {
                        UserId = user.Id,
                        Provider = provider,
                        ProviderKey = externalId,
                        CreatedAt = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Linked external login {Provider}:{ExternalId} to existing user {UserId}",
                        provider, externalId, user.Id);
                    return user;
                }
            }

            // Create new user
            user = await CreateExternalUserAsync(provider, claims);
            return user;
        }

        public async Task<User> FindUserByExternalIdAsync(string provider, string externalId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.ExternalType == provider && u.ExternalId == externalId);
        }

        public async Task<User> CreateExternalUserAsync(string provider, IEnumerable<Claim> claims)
        {
            var externalId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var firstName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value?.Split(' ').First();
            var lastName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value
                ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value?.Split(' ').Last()
                ?? "Unknown";
            var picture = claims.FirstOrDefault(c => c.Type == "picture")?.Value;

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Cannot create user without email from provider {Provider}", provider);
                return null;
            }

            var user = new User
            {
                Email = email,
                FirstName = firstName ?? "Unknown",
                LastName = lastName,
                ProfilePicture = picture,
                ExternalId = externalId,
                ExternalType = provider,
                ExternalSubject = claims.FirstOrDefault(c => c.Type == "sub")?.Value,
                ExternalIssuer = claims.FirstOrDefault(c => c.Type == "iss")?.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Add to external logins history
            _context.ExternalLogins.Add(new ExternalLogin
            {
                UserId = user.Id,
                Provider = provider,
                ProviderKey = externalId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await CreateEmptyVerificationRecord(user.Id);

            _logger.LogInformation("Created new user {UserId} from external login {Provider}:{ExternalId}",
                user.Id, provider, externalId);

            return user;
        }

        // Add this helper method
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
                _logger.LogInformation("Created empty verification record for external user {UserId}", userId);
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the external signup
                _logger.LogError(ex, "Failed to create empty verification record for external user {UserId}", userId);
            }
        }

    }
}