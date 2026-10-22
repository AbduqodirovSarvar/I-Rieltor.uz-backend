namespace Application.Interfaces.Dashboard
{
    /// <summary>Totals for the admin dashboard. Counts are keyed by the enum name (e.g. "ONGOING").</summary>
    public record DashboardSummary
    {
        public DateTime GeneratedAt { get; init; }
        public DashboardUsers Users { get; init; } = new();
        public Dictionary<string, int> PropertiesByStatus { get; init; } = [];
        public Dictionary<string, int> AuctionsByStatus { get; init; } = [];
        public int PendingApplications { get; init; }
        public DashboardPayments Payments { get; init; } = new();
        public DashboardSales Sales { get; init; } = new();
        public List<DashboardAuction> Ongoing { get; init; } = [];
        public List<DashboardAuction> Upcoming { get; init; } = [];
        public List<DashboardAwaitingPayment> AwaitingPayment { get; init; } = [];
        public List<DashboardDay> Last30Days { get; init; } = [];
    }

    public record DashboardUsers
    {
        public int Total { get; init; }
        public int NewLast7Days { get; init; }
        public Dictionary<string, int> ByType { get; init; } = [];
    }

    public record DashboardPayments
    {
        public int Pending { get; init; }
        public decimal PaidDeposits { get; init; }
        public decimal PaidFinalPayments { get; init; }
        public int PendingRefunds { get; init; }
        public decimal PendingRefundsAmount { get; init; }
    }

    public record DashboardSales
    {
        public int SoldCount { get; init; }
        public decimal Revenue { get; init; }
    }

    public record DashboardAuction
    {
        public Guid Id { get; init; }
        public string LotNumber { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public DateTime StartsAt { get; init; }
        public DateTime EndsAt { get; init; }
        public decimal? CurrentPrice { get; init; }
        public int Participants { get; init; }
        public int Bids { get; init; }
    }

    public record DashboardAwaitingPayment
    {
        public Guid AuctionId { get; init; }
        public string LotNumber { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Outcome { get; init; } = string.Empty;
        public decimal? FinalPrice { get; init; }
        public DateTime? PaymentDueAt { get; init; }
    }

    /// <summary>One UTC day; days without events are present with zeros.</summary>
    public record DashboardDay
    {
        public DateOnly Date { get; init; }
        public int NewUsers { get; init; }
        public int Bids { get; init; }
        public decimal PaidAmount { get; init; }
    }

    public interface IDashboardReader
    {
        /// <param name="listSize">How many rows the "ongoing", "upcoming" and "awaiting payment" lists hold.</param>
        Task<DashboardSummary> ReadAsync(DateTime now, CancellationToken cancellationToken, int listSize = 10);
    }
}
