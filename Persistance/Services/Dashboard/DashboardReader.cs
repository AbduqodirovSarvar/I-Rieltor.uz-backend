using Application.Interfaces.Dashboard;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using System.Linq.Expressions;

namespace Persistance.Services.Dashboard
{
    /// <summary>
    /// Aggregates straight from the tables (grouped counts and sums, not entities). Read-only; the caller caches the result.
    /// </summary>
    public class DashboardReader(AppDbContext db) : IDashboardReader
    {
        private const int Days = 30;

        public async Task<DashboardSummary> ReadAsync(DateTime now, CancellationToken cancellationToken, int listSize = 10)
        {
            // One context cannot run two queries at once, so the queries go one after another.
            var userTypes = await db.Users.AsNoTracking().GroupBy(u => u.Type).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
            var weekAgo = now.AddDays(-7);
            var newUsers = await db.Users.AsNoTracking().CountAsync(u => u.CreatedAt >= weekAgo, cancellationToken);

            var properties = await db.Properties.AsNoTracking().GroupBy(p => p.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
            var auctions = await db.Auctions.AsNoTracking().GroupBy(a => a.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
            var pending = await db.AuctionParticipants.AsNoTracking().CountAsync(p => p.Status == ParticipantStatus.PENDING, cancellationToken);

            var paid = await db.Payments.AsNoTracking().Where(p => p.Status == PaymentStatus.PAID && p.Purpose != PaymentPurpose.REFUND)
                .GroupBy(p => p.Purpose).Select(g => new { g.Key, Sum = g.Sum(p => p.Amount) }).ToListAsync(cancellationToken);
            var pendingPayments = await db.Payments.AsNoTracking().CountAsync(p => p.Status == PaymentStatus.PENDING && p.Purpose != PaymentPurpose.REFUND, cancellationToken);
            var refunds = await db.Payments.AsNoTracking().Where(p => p.Status == PaymentStatus.PENDING && p.Purpose == PaymentPurpose.REFUND)
                .GroupBy(_ => 1).Select(g => new { Count = g.Count(), Sum = g.Sum(p => p.Amount) }).FirstOrDefaultAsync(cancellationToken);

            var sold = await db.AuctionResults.AsNoTracking().Where(r => r.Outcome == AuctionOutcome.SOLD)
                .GroupBy(_ => 1).Select(g => new { Count = g.Count(), Sum = g.Sum(r => r.FinalPrice ?? 0m) }).FirstOrDefaultAsync(cancellationToken);

            var ongoing = await AuctionList(AuctionStatus.ONGOING, a => a.EndsAt, listSize, cancellationToken);
            var upcoming = await AuctionList(AuctionStatus.SCHEDULED, a => a.StartsAt, listSize, cancellationToken);

            var awaiting = await db.AuctionResults.AsNoTracking()
                .Where(r => r.Outcome == AuctionOutcome.AWAITING_PAYMENT || r.Outcome == AuctionOutcome.AWAITING_RUNNER_UP)
                .OrderBy(r => r.PaymentDueAt).Take(listSize)
                .Select(r => new DashboardAwaitingPayment
                {
                    AuctionId = r.AuctionId, LotNumber = r.Auction!.LotNumber, Title = r.Auction.Title,
                    Outcome = r.Outcome.ToString(), FinalPrice = r.FinalPrice, PaymentDueAt = r.PaymentDueAt,
                }).ToListAsync(cancellationToken);

            var series = await DaysAsync(now, cancellationToken);

            return new DashboardSummary
            {
                GeneratedAt = now,
                Users = new DashboardUsers
                {
                    Total = userTypes.Sum(t => t.Count), NewLast7Days = newUsers,
                    ByType = userTypes.ToDictionary(t => t.Key.ToString(), t => t.Count),
                },
                PropertiesByStatus = properties.ToDictionary(p => p.Key.ToString(), p => p.Count),
                AuctionsByStatus = auctions.ToDictionary(a => a.Key.ToString(), a => a.Count),
                PendingApplications = pending,
                Payments = new DashboardPayments
                {
                    Pending = pendingPayments,
                    PaidDeposits = paid.FirstOrDefault(p => p.Key == PaymentPurpose.DEPOSIT)?.Sum ?? 0m,
                    PaidFinalPayments = paid.FirstOrDefault(p => p.Key == PaymentPurpose.FINAL_PAYMENT)?.Sum ?? 0m,
                    PendingRefunds = refunds?.Count ?? 0, PendingRefundsAmount = refunds?.Sum ?? 0m,
                },
                Sales = new DashboardSales { SoldCount = sold?.Count ?? 0, Revenue = sold?.Sum ?? 0m },
                Ongoing = ongoing, Upcoming = upcoming, AwaitingPayment = awaiting, Last30Days = series,
            };
        }

        private async Task<List<DashboardAuction>> AuctionList(AuctionStatus status, Expression<Func<Auction, DateTime>> order, int listSize, CancellationToken cancellationToken)
            => await db.Auctions.AsNoTracking().Where(a => a.Status == status).OrderBy(order).Take(listSize)
                .Select(a => new DashboardAuction
                {
                    Id = a.Id, LotNumber = a.LotNumber, Title = a.Title, StartsAt = a.StartsAt, EndsAt = a.EndsAt, CurrentPrice = a.CurrentPrice,
                    Participants = db.AuctionParticipants.Count(p => p.AuctionId == a.Id && p.Status == ParticipantStatus.APPROVED),
                    Bids = db.Bids.Count(b => b.AuctionId == a.Id),
                }).ToListAsync(cancellationToken);

        private async Task<List<DashboardDay>> DaysAsync(DateTime now, CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(now);
            var first = today.AddDays(-(Days - 1));
            var from = first.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var users = await db.Users.AsNoTracking().Where(u => u.CreatedAt >= from)
                .GroupBy(u => u.CreatedAt.Date).Select(g => new { Day = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
            var bids = await db.Bids.AsNoTracking().Where(b => b.CreatedAt >= from)
                .GroupBy(b => b.CreatedAt.Date).Select(g => new { Day = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
            var money = await db.Payments.AsNoTracking().Where(p => p.Status == PaymentStatus.PAID && p.Purpose != PaymentPurpose.REFUND && p.PaidAt >= from)
                .GroupBy(p => p.PaidAt!.Value.Date).Select(g => new { Day = g.Key, Sum = g.Sum(p => p.Amount) }).ToListAsync(cancellationToken);

            return Enumerable.Range(0, Days).Select(i =>
            {
                var date = first.AddDays(i);
                var day = date.ToDateTime(TimeOnly.MinValue);
                return new DashboardDay
                {
                    Date = date,
                    NewUsers = users.FirstOrDefault(u => u.Day == day)?.Count ?? 0,
                    Bids = bids.FirstOrDefault(b => b.Day == day)?.Count ?? 0,
                    PaidAmount = money.FirstOrDefault(m => m.Day == day)?.Sum ?? 0m,
                };
            }).ToList();
        }
    }
}
