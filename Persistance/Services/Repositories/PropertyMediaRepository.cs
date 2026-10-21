using Application.Interfaces.Cachings;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using Persistance.Services.Repositories.Abstractions;

namespace Persistance.Services.Repositories
{
    public class PropertyMediaRepository(AppDbContext context, IRedisService redisService, IUnitOfWork unitOfWork)
        : GenericRepository<PropertyMedia>(context, redisService, unitOfWork), IPropertyMediaRepository
    {    }
}
