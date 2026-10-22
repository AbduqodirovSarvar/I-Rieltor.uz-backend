using Application.Exceptions;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.Auths;
using Application.Interfaces.Cachings;
using Application.Interfaces.Dashboard;
using Application.Models.Results;

namespace Application.Features.Dashboard
{
    /// <summary>Admin dashboard totals; cached for half a minute so a busy dashboard does not load the database.</summary>
    public record GetDashboardSummaryQuery : IQuery<ResponseModel<DashboardSummary>>;

    public class GetDashboardSummaryQueryHandler(IDashboardReader reader, IRedisService cache, IPermissionService permissionService)
        : IQueryHandler<GetDashboardSummaryQuery, ResponseModel<DashboardSummary>>
    {
        private const string CacheKey = "v1:dashboard:summary";
        private static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(30);

        public async Task<ResponseModel<DashboardSummary>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
        {
            if (!await permissionService.Check("READ_DASHBOARD"))
                throw new ForbiddenException("You do not have permission to read this entity.");

            var cached = await cache.GetAsync<DashboardSummary>(CacheKey);
            if (cached != null)
                return ResponseModel<DashboardSummary>.Ok(cached);

            var summary = await reader.ReadAsync(DateTime.UtcNow, cancellationToken);
            await cache.SetIfNotExistsAsync(CacheKey, summary, CacheFor);
            return ResponseModel<DashboardSummary>.Ok(summary);
        }
    }
}
