using Application.Interfaces.Auctions;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs
{
    /// <summary>
    /// Live view of auctions at <c>/hubs/auction</c>. Anyone may watch (the same data as the public pages); nothing can be
    /// done through the hub: bids are placed with POST api/Bid. A client calls <c>Join(auctionId)</c> and then receives
    /// <c>BidPlaced</c> and <c>AuctionStatusChanged</c> for that auction.
    /// </summary>
    public sealed class AuctionHub : Hub
    {
        // A page watches one auction, a list a handful; the cap keeps a client from collecting groups.
        private const int MaxGroupsPerConnection = 20;
        private const string GroupsItem = "groups";

        public static string GroupOf(Guid auctionId) => $"auction-{auctionId:N}";

        public async Task Join(Guid auctionId)
        {
            var groups = (HashSet<Guid>)Context.Items.GetOrAdd(GroupsItem, _ => new HashSet<Guid>());
            lock (groups)
            {
                if (!groups.Contains(auctionId) && groups.Count >= MaxGroupsPerConnection)
                    throw new HubException($"At most {MaxGroupsPerConnection} auctions can be watched at once.");
                groups.Add(auctionId);
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupOf(auctionId));
        }

        public async Task Leave(Guid auctionId)
        {
            if (Context.Items.TryGetValue(GroupsItem, out var value) && value is HashSet<Guid> groups)
                lock (groups)
                    groups.Remove(auctionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupOf(auctionId));
        }
    }

    internal static class DictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> create)
        {
            if (!dictionary.TryGetValue(key, out var value))
                dictionary[key] = value = create(key);
            return value;
        }
    }

    /// <summary>Pushes the events to the watchers of the auction. A failure to push is logged and ignored.</summary>
    public sealed class SignalRAuctionEvents(IHubContext<AuctionHub> hub, ILogger<SignalRAuctionEvents> logger) : IAuctionEvents
    {
        public Task BidPlacedAsync(BidPlacedEvent bidPlaced) => SendAsync(bidPlaced.AuctionId, "BidPlaced", bidPlaced);

        public Task StatusChangedAsync(AuctionStatusChangedEvent statusChanged) => SendAsync(statusChanged.AuctionId, "AuctionStatusChanged", statusChanged);

        private async Task SendAsync(Guid auctionId, string method, object payload)
        {
            try
            {
                await hub.Clients.Group(AuctionHub.GroupOf(auctionId)).SendAsync(method, payload);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "{Event} for auction {AuctionId} could not be pushed", method, auctionId);
            }
        }
    }
}
