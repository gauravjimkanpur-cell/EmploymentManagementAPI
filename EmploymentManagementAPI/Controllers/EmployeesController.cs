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
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeesController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees([FromQuery] string? search, [FromQuery] string? department, [FromQuery] bool? isActive)
        {
            var result = await _employeeService.GetEmployeesAsync(search, department, isActive);
            
            // Get current user's role from token
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Employee";
            var currentUsername = User.Identity?.Name ?? string.Empty;
            
            // If not Admin, show only current user's data
            if (userRole != "Admin")
            {
                result = result.Where(e => e.EmployeeCode.Equals(currentUsername, System.StringComparison.OrdinalIgnoreCase));
            }
            
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            var dto = await _employeeService.GetEmployeeByIdAsync(id);
            if (dto == null)
            {
                return NotFound(new { message = $"Employee with ID {id} not found." });
            }
            
            // Verify access: only Admin or the employee themselves can view
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Employee";
            var currentUsername = User.Identity?.Name ?? string.Empty;
            
            if (userRole != "Admin" && !dto.EmployeeCode.Equals(currentUsername, System.StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }
            
            return Ok(dto);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] EmployeeCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _employeeService.CodeExistsAsync(dto.EmployeeCode))
                return BadRequest(new { message = "Employee Code is already in use." });

            if (await _employeeService.EmailExistsAsync(dto.Email))
                return BadRequest(new { message = "Email is already in use." });

            string createdBy = User.Identity?.Name ?? "Admin";

            var result = await _employeeService.CreateEmployeeAsync(dto, createdBy);
            return CreatedAtAction(nameof(GetEmployeeById), new { id = result.EmployeeId }, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] EmployeeCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _employeeService.CodeExistsAsync(dto.EmployeeCode, id))
                return BadRequest(new { message = "Employee Code is already in use by another employee." });

            if (await _employeeService.EmailExistsAsync(dto.Email, id))
                return BadRequest(new { message = "Email is already in use by another employee." });

            string modifiedBy = User.Identity?.Name ?? "Admin";

            var updated = await _employeeService.UpdateEmployeeAsync(id, dto, modifiedBy);
            if (!updated)
            {
                return NotFound(new { message = $"Employee with ID {id} not found." });
            }

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var deleted = await _employeeService.DeleteEmployeeAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Employee with ID {id} not found." });
            }

            return Ok(new { message = "Employee deleted successfully." });
        }
    }
}
