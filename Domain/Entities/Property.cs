using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Property : AudiTableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public PropertyCategory? Category { get; set; } = null;
        public decimal InitialPrice { get; set; }
        public double ViewCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
    }
}
