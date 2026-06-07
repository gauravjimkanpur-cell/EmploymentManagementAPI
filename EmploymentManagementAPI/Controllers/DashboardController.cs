using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmployeeManagement.DTOs;
using EmployeeManagement.Services.Interfaces;

namespace EmployeeManagement.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public DashboardController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var employees = await _employeeService.GetEmployeesAsync(null, null, null);
            var employeeList = employees.ToList();
            
            // Get current user's role and username from token
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Employee";
            var currentUsername = User.Identity?.Name ?? string.Empty;
            
            // If not Admin, show stats for only the current user
            if (userRole != "Admin")
            {
                employeeList = employeeList
                    .Where(e => e.EmployeeCode.Equals(currentUsername, System.StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var stats = new DashboardStatsDto
            {
                TotalEmployees = employeeList.Count,
                ActiveEmployees = employeeList.Count(e => e.IsActive),
                InactiveEmployees = employeeList.Count(e => !e.IsActive)
            };

            return Ok(stats);
        }
    }
}
