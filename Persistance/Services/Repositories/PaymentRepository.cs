using Application.Interfaces.Cachings;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Persistance.Data;
using Persistance.Services.Repositories.Abstractions;

namespace Persistance.Services.Repositories
{
    public class PaymentRepository(AppDbContext context, IRedisService redisService, IUnitOfWork unitOfWork)
        : GenericRepository<Payment>(context, redisService, unitOfWork), IPaymentRepository
    {
        // Read by the owner or staff rarely and always checked for access; not worth a cache entry that could outlive a delete.
        protected override bool UseCache => false;
    }
}
