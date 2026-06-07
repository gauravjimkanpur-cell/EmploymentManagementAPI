using System.Collections.Generic;
using System.Threading.Tasks;
using EmployeeManagement.DTOs;

namespace EmployeeManagement.Services.Interfaces
{
    public interface IEmployeeService
    {
        Task<IEnumerable<EmployeeDto>> GetEmployeesAsync(string? search, string? department, bool? isActive);
        Task<EmployeeDto?> GetEmployeeByIdAsync(int id);
        Task<EmployeeDto> CreateEmployeeAsync(EmployeeCreateDto dto, string createdBy);
        Task<bool> UpdateEmployeeAsync(int id, EmployeeCreateDto dto, string modifiedBy);
        Task<bool> DeleteEmployeeAsync(int id);
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
        Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    }
}
