using System;
using System.Linq;
using System.Threading.Tasks;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.DTOs;
using EmployeeManagement.Domain.Factories.Interfaces;
using EmployeeManagement.Infrastructure.Repositories.Interfaces;
using EmployeeManagement.Infrastructure.Services.Interfaces;
using EmployeeManagement.Services.Interfaces;

namespace EmployeeManagement.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public UserService(
            IRepositoryFactory repositoryFactory,
            IPasswordHasher passwordHasher,
            ITokenService tokenService)
        {
            _userRepository = repositoryFactory.Create<User>();
            _employeeRepository = repositoryFactory.Create<Employee>();
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<AuthResponseDto?> AuthenticateAsync(LoginDto loginDto)
        {
            var usernameInput = loginDto.Username.Trim();
            var users = await _userRepository.FindAsync(u => u.Username.ToUpper() == usernameInput.ToUpper());
            var user = users.FirstOrDefault();

            if (user == null)
            {
                var employees = await _employeeRepository.FindAsync(e => e.Email == loginDto.Username.ToLower().Trim());
                var employee = employees.FirstOrDefault();
                if (employee != null)
                {
                    users = await _userRepository.FindAsync(u => u.Username == employee.EmployeeCode);
                    user = users.FirstOrDefault();
                }
            }

            if (user == null || !_passwordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return null;
            }

            var token = _tokenService.GenerateToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _userRepository.ExecuteInTransactionAsync(async () =>
            {
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();
            });

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                Username = user.Username,
                Role = user.Role
            };
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(TokenRequestDto tokenRequestDto)
        {
            if (string.IsNullOrEmpty(tokenRequestDto.RefreshToken))
            {
                return null;
            }

            var users = await _userRepository.FindAsync(u => u.RefreshToken == tokenRequestDto.RefreshToken);
            var user = users.FirstOrDefault();
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null;
            }

            var newAccessToken = _tokenService.GenerateToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _userRepository.ExecuteInTransactionAsync(async () =>
            {
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();
            });

            return new AuthResponseDto
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                Username = user.Username,
                Role = user.Role
            };
        }

        public async Task<bool> RevokeTokenAsync(string username)
        {
            var users = await _userRepository.FindAsync(u => u.Username == username);
            var user = users.FirstOrDefault();
            if (user == null)
            {
                return false;
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _userRepository.ExecuteInTransactionAsync(async () =>
            {
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();
            });

            return true;
        }

        public async Task<(bool Succeeded, string ErrorMessage)> ChangePasswordAsync(string username, ChangePasswordDto dto)
        {
            var users = await _userRepository.FindAsync(u => u.Username == username);
            var user = users.FirstOrDefault();
            if (user == null)
            {
                return (false, "User not found.");
            }

            if (!_passwordHasher.VerifyPassword(dto.OldPassword, user.PasswordHash))
            {
                return (false, "Incorrect current password.");
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                return (false, "New password and confirmation password do not match.");
            }

            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);

            await _userRepository.ExecuteInTransactionAsync(async () =>
            {
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();
            });

            return (true, string.Empty);
        }
    }
}
