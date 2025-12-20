using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class AuditLog : AudiTableEntity
    {
        public string ActionName { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string EntityTypeName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent {  get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }
    }
}
