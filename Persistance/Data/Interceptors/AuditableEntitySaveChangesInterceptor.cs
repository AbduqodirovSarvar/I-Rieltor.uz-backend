using Application.Interfaces.Auths;
using Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Data.Interceptors
{
    public class AuditableEntitySaveChangesInterceptor(IServiceProvider serviceProvider) 
        : SaveChangesInterceptor
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public AuditableEntitySaveChangesInterceptor()
        {
        }

        private ICurrentUserService? GetCurrentUserService()
        {
            return _serviceProvider.GetService<ICurrentUserService>();
        }
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }


        private void UpdateEntities(DbContext? context)
        {
            if (context == null) return;
            var currentUserService = GetCurrentUserService();

            foreach (var entry in context.ChangeTracker.Entries<AudiTableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    if (currentUserService != null)
                        entry.Entity.CreatedBy ??= currentUserService.UserId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    if (currentUserService != null)
                        entry.Entity.UpdatedBy ??= currentUserService?.UserId;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
