using Application.Exceptions;
using Application.Extensions;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.Auths;
using Application.Interfaces.Repositories;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Notifications;
using Domain.Entities;
using Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Notifications
{
    /// <summary>The signed-in user's own messages, newest first (the last 50 that were actually sent).</summary>
    public record GetMyNotificationsQuery : IQuery<ResponseModel<List<MyNotificationViewModel>>>;

    public class GetMyNotificationsQueryHandler(INotificationRepository notifications, ICurrentUserService currentUser)
        : IQueryHandler<GetMyNotificationsQuery, ResponseModel<List<MyNotificationViewModel>>>
    {
        private const int Limit = 50;

        public async Task<ResponseModel<List<MyNotificationViewModel>>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new UnauthenticatedException("Sign in first.");

            // A message about one event goes out on every channel the person has (e-mail and SMS): twice the rows are read,
            // and the pairs are folded into one below.
            var (rows, _) = await notifications.GetPaginatedAsync(
                n => n.UserId == userId && n.Status == NotificationStatus.SENT, 0, Limit * 2,
                q => q.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id), cancellationToken);

            // Rows of one event are created together; a row of another channel, of the same kind, within a couple of seconds
            // of one already taken is its pair. The e-mail is kept: it has the subject and the longer text.
            var kept = new List<Notification>();
            foreach (var n in rows.OrderByDescending(n => n.CreatedAt).ThenBy(n => n.Channel).ThenByDescending(n => n.Id))
            {
                var pair = kept.FindIndex(k => k.Type == n.Type && k.AuctionId == n.AuctionId && k.Channel != n.Channel
                                               && Math.Abs((k.CreatedAt - n.CreatedAt).TotalSeconds) < 3);
                if (pair < 0)
                    kept.Add(n);
                else if (n.Channel == NotificationChannel.EMAIL)
                    kept[pair] = n;
            }

            var folded = kept
                .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
                .Take(Limit)
                .Select(n => new MyNotificationViewModel
                {
                    Id = n.Id, CreatedAt = n.CreatedAt, Type = n.Type, Subject = n.Subject, Body = n.Body, AuctionId = n.AuctionId
                })
                .ToList();
            return ResponseModel<List<MyNotificationViewModel>>.Ok(folded);
        }
    }

    /// <summary>The delivery log: what was sent to whom, and what failed.</summary>
    public record GetNotificationListQuery : BaseGetListQuery, IQuery<ResponseModel<List<NotificationViewModel>>>;

    public class GetNotificationListQueryHandler(INotificationRepository notifications, IPermissionService permissionService, IUserDirectory users, IMapper mapper)
        : IQueryHandler<GetNotificationListQuery, ResponseModel<List<NotificationViewModel>>>
    {
        public async Task<ResponseModel<List<NotificationViewModel>>> Handle(GetNotificationListQuery request, CancellationToken cancellationToken)
        {
            if (!await permissionService.Check("READ_NOTIFICATION"))
                throw new ForbiddenException("You do not have permission to read this entity.");

            var (data, total) = await notifications.GetPaginatedAsync(
                request.BuildPredicate<Notification>(), request.PageIndex, request.PageSize,
                request.BuildOrderBy<Notification>() ?? (q => q.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)), cancellationToken);
            var views = mapper.Map<List<NotificationViewModel>>(data);
            var people = await users.GetAsync(views.Select(v => (Guid?)v.UserId), cancellationToken);
            foreach (var view in views)
                view.UserFullName = people.GetValueOrDefault(view.UserId)?.FullName;
            return ResponseModel<List<NotificationViewModel>>.Ok(views, request.PageIndex, request.PageSize, total);
        }
    }
}
