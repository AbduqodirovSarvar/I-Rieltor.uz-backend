using Application.Exceptions;
using Application.Extensions;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.Auths;
using Application.Interfaces.Repositories;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Notifications;
using Domain.Entities;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Notifications
{
    /// <summary>The delivery log: what was sent to whom, and what failed.</summary>
    public record GetNotificationListQuery : BaseGetListQuery, IQuery<ResponseModel<List<NotificationViewModel>>>;

    public class GetNotificationListQueryHandler(INotificationRepository notifications, IPermissionService permissionService, IMapper mapper)
        : IQueryHandler<GetNotificationListQuery, ResponseModel<List<NotificationViewModel>>>
    {
        public async Task<ResponseModel<List<NotificationViewModel>>> Handle(GetNotificationListQuery request, CancellationToken cancellationToken)
        {
            if (!await permissionService.Check("READ_NOTIFICATION"))
                throw new ForbiddenException("You do not have permission to read this entity.");

            var (data, total) = await notifications.GetPaginatedAsync(
                request.BuildPredicate<Notification>(), request.PageIndex, request.PageSize,
                request.BuildOrderBy<Notification>() ?? (q => q.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)), cancellationToken);
            return ResponseModel<List<NotificationViewModel>>.Ok(mapper.Map<List<NotificationViewModel>>(data), request.PageIndex, request.PageSize, total);
        }
    }
}
