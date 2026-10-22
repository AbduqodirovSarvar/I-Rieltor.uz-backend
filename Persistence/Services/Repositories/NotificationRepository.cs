using Application.Interfaces.Cachings;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Persistence.Data;
using Persistence.Services.Repositories.Abstractions;

namespace Persistence.Services.Repositories
{
    public class NotificationRepository(AppDbContext context, IRedisService redisService, IUnitOfWork unitOfWork)
        : GenericRepository<Notification>(context, redisService, unitOfWork), INotificationRepository
    {
        // Rows change on every attempt and are read by the dispatcher straight from the database.
        protected override bool UseCache => false;
    }
}
