using EmployeeManagement.Domain.Factories.Interfaces;
using EmployeeManagement.Infrastructure.Data;
using EmployeeManagement.Infrastructure.Repositories;
using EmployeeManagement.Infrastructure.Repositories.Interfaces;

namespace EmployeeManagement.Domain.Factories
{
    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly ApplicationDbContext _context;

        public RepositoryFactory(ApplicationDbContext context)
        {
            _context = context;
        }

        public IRepository<T> Create<T>() where T : class
        {
            return new Repository<T>(_context);
        }
    }
}
