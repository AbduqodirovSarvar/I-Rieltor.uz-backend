using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class AuctionApplications : AudiTableEntity
    {
        public Guid AuctionId { get; set; }
        public Auction? Auction { get; set; } = null;
        public Guid ApplicantId { get; set; }
        public User? Applicant { get; set; } = null;
        public Guid ApplicationFileId { get; set; }
        public UploadedFile? ApplicationFile { get; set; } = null;
        public decimal OfferedAmount { get; set; }
        public string ApplicationStatus { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = false;
        public DateTime? ApprovedAt { get; set; }
    }
}
