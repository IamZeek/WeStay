using WeStay.AuthService.Models;
using System.Security.Claims;

namespace WeStay.AuthService.Services.Interfaces
{
    public interface IExternalAuthService
    {
        Task<User> HandleExternalLoginAsync(string provider, IEnumerable<Claim> claims);
        Task<User> FindUserByExternalIdAsync(string provider, string externalId);
        Task<User> CreateExternalUserAsync(string provider, IEnumerable<Claim> claims);
    }
}