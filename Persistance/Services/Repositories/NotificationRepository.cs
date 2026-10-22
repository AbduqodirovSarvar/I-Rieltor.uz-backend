using Application.Interfaces.Cachings;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Persistance.Data;
using Persistance.Services.Repositories.Abstractions;

namespace Persistance.Services.Repositories
{
    public class NotificationRepository(AppDbContext context, IRedisService redisService, IUnitOfWork unitOfWork)
        : GenericRepository<Notification>(context, redisService, unitOfWork), INotificationRepository
    {
        // Rows change on every attempt and are read by the dispatcher straight from the database.
        protected override bool UseCache => false;
    }
}
