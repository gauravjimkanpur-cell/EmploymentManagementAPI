using AutoMapper;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.DTOs;

namespace EmployeeManagement.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Employee, EmployeeDto>()
                .ForMember(dest => dest.Role, opt => opt.Ignore());
            CreateMap<EmployeeCreateDto, Employee>();
        }
    }
}
