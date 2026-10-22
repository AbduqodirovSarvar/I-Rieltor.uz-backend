using Application.Exceptions;
using Application.Extensions;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.Auths;
using Application.Interfaces.Repositories;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.AuditLogs;
using Domain.Entities;
using MapsterMapper;

namespace Application.Features.AuditLogs
{
    /// <summary>
    /// The audit log, newest first. Filters, e.g. <c>Filters[userId.equals]=...</c>, <c>Filters[statusCode.greaterthan]=399</c>,
    /// <c>Filters[occurredAt.greaterthan]=2026-10-01T00:00:00Z</c>, <c>Filters[method.equals]=DELETE</c>.
    /// </summary>
    public record GetAuditLogListQuery : BaseGetListQuery, IQuery<ResponseModel<List<AuditLogViewModel>>>;

    public class GetAuditLogListQueryHandler(IAuditLogRepository auditLogs, IPermissionService permissionService, IMapper mapper)
        : IQueryHandler<GetAuditLogListQuery, ResponseModel<List<AuditLogViewModel>>>
    {
        public async Task<ResponseModel<List<AuditLogViewModel>>> Handle(GetAuditLogListQuery request, CancellationToken cancellationToken)
        {
            if (!await permissionService.Check("READ_AUDITLOG"))
                throw new ForbiddenException("You do not have permission to read this entity.");

            var (data, total) = await auditLogs.GetPaginatedAsync(
                request.BuildPredicate<AuditLog>(), request.PageIndex, request.PageSize,
                request.BuildOrderBy<AuditLog>() ?? (q => q.OrderByDescending(a => a.OccurredAt).ThenByDescending(a => a.Id)), cancellationToken);
            return ResponseModel<List<AuditLogViewModel>>.Ok(mapper.Map<List<AuditLogViewModel>>(data), request.PageIndex, request.PageSize, total);
        }
    }
}
