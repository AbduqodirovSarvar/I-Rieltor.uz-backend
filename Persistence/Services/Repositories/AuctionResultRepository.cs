using Application.Interfaces.Cachings;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Persistence.Data;
using Persistence.Services.Repositories.Abstractions;

namespace Persistence.Services.Repositories
{
    public class AuctionResultRepository(AppDbContext context, IRedisService redisService, IUnitOfWork unitOfWork)
        : GenericRepository<AuctionResult>(context, redisService, unitOfWork), IAuctionResultRepository
    {
    }
}
