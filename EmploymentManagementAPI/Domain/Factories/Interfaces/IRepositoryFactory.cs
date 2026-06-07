using EmployeeManagement.Infrastructure.Repositories.Interfaces;

namespace EmployeeManagement.Domain.Factories.Interfaces
{
    public interface IRepositoryFactory
    {
        IRepository<T> Create<T>() where T : class;
    }
}
