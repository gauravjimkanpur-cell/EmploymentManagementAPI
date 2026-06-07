using System.Security.Claims;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
