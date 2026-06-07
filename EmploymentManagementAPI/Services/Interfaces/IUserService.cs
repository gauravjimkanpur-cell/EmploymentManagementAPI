using System.Threading.Tasks;
using EmployeeManagement.DTOs;

namespace EmployeeManagement.Services.Interfaces
{
    public interface IUserService
    {
        Task<AuthResponseDto?> AuthenticateAsync(LoginDto loginDto);
        Task<AuthResponseDto?> RefreshTokenAsync(TokenRequestDto tokenRequestDto);
        Task<bool> RevokeTokenAsync(string username);
        Task<(bool Succeeded, string ErrorMessage)> ChangePasswordAsync(string username, ChangePasswordDto dto);
    }
}
