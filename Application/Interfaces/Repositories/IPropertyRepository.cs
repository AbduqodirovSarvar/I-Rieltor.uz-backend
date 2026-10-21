using Application.Interfaces.Repositories.Abstractions;
using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IPropertyRepository
        : IGenericRepository<Property>
    {
        /// <summary>
        /// +1 in one UPDATE, without loading the row (concurrent page views do not overwrite each other).
        /// </summary>
        Task IncrementViewCountAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
