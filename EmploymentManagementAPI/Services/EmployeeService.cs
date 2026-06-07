using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Factories.Interfaces;
using EmployeeManagement.DTOs;
using EmployeeManagement.Infrastructure.Repositories.Interfaces;
using EmployeeManagement.Infrastructure.Services.Interfaces;
using EmployeeManagement.Services.Interfaces;

namespace EmployeeManagement.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IMapper _mapper;

        public EmployeeService(
            IRepositoryFactory repositoryFactory,
            IPasswordHasher passwordHasher,
            IMapper mapper)
        {
            _employeeRepository = repositoryFactory.Create<Employee>();
            _userRepository = repositoryFactory.Create<User>();
            _passwordHasher = passwordHasher;
            _mapper = mapper;
        }

        public async Task<IEnumerable<EmployeeDto>> GetEmployeesAsync(string? search, string? department, bool? isActive)
        {
            var employees = await _employeeRepository.GetAllAsync();
            var query = employees.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLowerInvariant();
                query = query.Where(e =>
                    e.FirstName.ToLowerInvariant().Contains(s) ||
                    e.LastName.ToLowerInvariant().Contains(s) ||
                    e.EmployeeCode.ToLowerInvariant().Contains(s) ||
                    e.Email.ToLowerInvariant().Contains(s)
                );
            }

            if (!string.IsNullOrWhiteSpace(department))
            {
                string d = department.Trim().ToLowerInvariant();
                query = query.Where(e => e.Department.ToLowerInvariant() == d);
            }

            if (isActive.HasValue)
            {
                query = query.Where(e => e.IsActive == isActive.Value);
            }

            var dtos = _mapper.Map<List<EmployeeDto>>(query.ToList());
            var users = await _userRepository.GetAllAsync();
            var userMap = users.ToDictionary(u => u.Username, u => u.Role, StringComparer.OrdinalIgnoreCase);
            foreach (var dto in dtos)
            {
                if (userMap.TryGetValue(dto.EmployeeCode, out var role))
                {
                    dto.Role = role;
                }
            }
            return dtos;
        }

        public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee == null)
            {
                return null;
            }
            var dto = _mapper.Map<EmployeeDto>(employee);
            var users = await _userRepository.FindAsync(u => u.Username == employee.EmployeeCode);
            var user = users.FirstOrDefault();
            if (user != null)
            {
                dto.Role = user.Role;
            }
            return dto;
        }

        public async Task<EmployeeDto> CreateEmployeeAsync(EmployeeCreateDto dto, string createdBy)
        {
            var employee = new Employee
            {
                EmployeeCode = dto.EmployeeCode.Trim(),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                Department = dto.Department.Trim(),
                Designation = dto.Designation.Trim(),
                JoiningDate = dto.JoiningDate,
                IsActive = dto.IsActive,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = createdBy.Trim()
            };

            var user = new User
            {
                Username = dto.EmployeeCode.Trim(),
                PasswordHash = _passwordHasher.HashPassword(dto.Password ?? "DefaultPassword123!"),
                Role = dto.Role ?? "User",
                CreatedOn = DateTime.UtcNow
            };

            await _employeeRepository.ExecuteInTransactionAsync(async () =>
            {
                await _employeeRepository.AddAsync(employee);
                await _userRepository.AddAsync(user);
                await _employeeRepository.SaveChangesAsync();
                await _userRepository.SaveChangesAsync();
            });

            var result = _mapper.Map<EmployeeDto>(employee);
            result.Role = user.Role;
            return result;
        }

        public async Task<bool> UpdateEmployeeAsync(int id, EmployeeCreateDto dto, string modifiedBy)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee == null)
            {
                return false;
            }

            var oldCode = employee.EmployeeCode;

            _mapper.Map(dto, employee);
            employee.ModifiedOn = DateTime.UtcNow;
            employee.ModifiedBy = modifiedBy;

            var users = await _userRepository.FindAsync(u => u.Username == oldCode);
            var user = users.FirstOrDefault();

            if (user == null)
            {
                user = new User
                {
                    Username = employee.EmployeeCode.Trim(),
                    PasswordHash = _passwordHasher.HashPassword(dto.Password ?? "DefaultPassword123!"),
                    Role = dto.Role ?? "User",
                    CreatedOn = DateTime.UtcNow
                };
            }
            else
            {
                user.Username = employee.EmployeeCode.Trim();
                user.Role = dto.Role ?? "User";
                if (!string.IsNullOrEmpty(dto.Password))
                {
                    user.PasswordHash = _passwordHasher.HashPassword(dto.Password);
                }
            }

            await _employeeRepository.ExecuteInTransactionAsync(async () =>
            {
                _employeeRepository.Update(employee);
                if (user.UserId == 0)
                {
                    await _userRepository.AddAsync(user);
                }
                else
                {
                    _userRepository.Update(user);
                }
                await _employeeRepository.SaveChangesAsync();
                await _userRepository.SaveChangesAsync();
            });

            return true;
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee == null)
            {
                return false;
            }

            var users = await _userRepository.FindAsync(u => u.Username == employee.EmployeeCode);
            var user = users.FirstOrDefault();

            await _employeeRepository.ExecuteInTransactionAsync(async () =>
            {
                _employeeRepository.Delete(employee);
                if (user != null)
                {
                    _userRepository.Delete(user);
                }
                await _employeeRepository.SaveChangesAsync();
                await _userRepository.SaveChangesAsync();
            });

            return true;
        }

        public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                var matches = await _employeeRepository.FindAsync(e => e.EmployeeCode == code && e.EmployeeId != excludeId.Value);
                return matches.Any();
            }
            else
            {
                var matches = await _employeeRepository.FindAsync(e => e.EmployeeCode == code);
                return matches.Any();
            }
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        {
            string emailLower = email.Trim().ToLowerInvariant();
            if (excludeId.HasValue)
            {
                var matches = await _employeeRepository.FindAsync(e => e.Email == emailLower && e.EmployeeId != excludeId.Value);
                return matches.Any();
            }
            else
            {
                var matches = await _employeeRepository.FindAsync(e => e.Email == emailLower);
                return matches.Any();
            }
        }
    }
}
