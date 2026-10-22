using Application.Interfaces.Cachings;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Persistence.Data;
using Persistence.Services.Repositories.Abstractions;

namespace Persistence.Services.Repositories
{
    public class AuctionParticipantRepository(AppDbContext context, IRedisService redisService, IUnitOfWork unitOfWork)
        : GenericRepository<AuctionParticipant>(context, redisService, unitOfWork), IAuctionParticipantRepository
    {
    }
}
