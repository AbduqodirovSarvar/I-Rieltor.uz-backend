using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Requests.Abstractions
{
    public record BaseGetListQuery
    {
        public int PageIndex { get; init; } = 0;
        public int PageSize { get; init; } = 20;
        public string? SearchText { get; init; } = null;
        public string? SortBy { get; init; } = "CreatedAt";
        public string SortDirection { get; init; } = "desc";
        public Dictionary<string, string> Filters { get; init; } = [];
    }
}
