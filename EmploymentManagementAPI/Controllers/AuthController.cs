using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmployeeManagement.DTOs;
using EmployeeManagement.Services.Interfaces;

namespace EmployeeManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.AuthenticateAsync(loginDto);
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }

            var refreshCookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Expires = System.DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("X-Refresh-Token", result.RefreshToken, refreshCookieOptions);

            return Ok(new { token = result.Token, username = result.Username, role = result.Role });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                await _userService.RevokeTokenAsync(username);
            }
            Response.Cookies.Delete("X-Refresh-Token");
            return Ok(new { message = "Logged out successfully." });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["X-Refresh-Token"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest(new { message = "Invalid client request." });
            }

            var result = await _userService.RefreshTokenAsync(new TokenRequestDto
            {
                AccessToken = string.Empty,
                RefreshToken = refreshToken
            });
            if (result == null)
            {
                return BadRequest(new { message = "Invalid or expired token." });
            }

            var refreshCookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Expires = System.DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("X-Refresh-Token", result.RefreshToken, refreshCookieOptions);

            return Ok(new { token = result.Token, username = result.Username, role = result.Role });
        }

        [Authorize]
        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest();
            }

            var result = await _userService.RevokeTokenAsync(username);
            if (!result)
            {
                return NotFound();
            }

            Response.Cookies.Delete("X-Refresh-Token");
            return NoContent();
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var (succeeded, errorMessage) = await _userService.ChangePasswordAsync(username, dto);
            if (!succeeded)
            {
                return BadRequest(new { message = errorMessage });
            }

            return Ok(new { message = "Password changed successfully." });
        }
    }
}
