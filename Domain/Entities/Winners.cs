using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Winners : AudiTableEntity
    {
        public Guid AuctionId { get; set; }
        public Auction? Auction { get; set; } = null;
        public Guid WinnerId { get; set; }
        public User? Winner { get; set; } = null;
        public decimal WinningBid { get; set; }
        public decimal RemainingPayment { get; set; }
        public decimal TotalPayment { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }
}
