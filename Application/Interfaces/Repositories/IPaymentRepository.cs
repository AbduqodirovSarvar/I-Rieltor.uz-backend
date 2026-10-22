using Application.Interfaces.Repositories.Abstractions;
using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IPaymentRepository
        : IGenericRepository<Payment>
    {
    }
}
