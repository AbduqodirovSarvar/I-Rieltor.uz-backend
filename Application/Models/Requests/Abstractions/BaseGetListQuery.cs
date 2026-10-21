using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Requests.Abstractions
{
    public record BaseGetListQuery
    {
        public const int MaxPageSize = 100;
        public const int DefaultPageSize = 20;

        /// <summary>
        /// Zero-based page number.
        /// </summary>
        /// <remarks>Capped so PageIndex * PageSize (the SQL OFFSET) cannot overflow int.</remarks>
        [Range(0, MaxPageIndex)]
        public int PageIndex { get; init; } = 0;
        public const int MaxPageIndex = 1_000_000;

        /// <summary>
        /// Items per page; capped so one request cannot load a whole table.
        /// </summary>
        [Range(1, MaxPageSize)]
        public int PageSize { get; init; } = DefaultPageSize;

        /// <summary>
        /// Case-insensitive text looked for in the entity's searchable fields (name, title, login...).
        /// </summary>
        [MaxLength(100)]
        public string? SearchText { get; init; } = null;

        /// <summary>
        /// Property to sort by (e.g. "CreatedAt", "Title"). Empty: the list's default order.
        /// </summary>
        [MaxLength(64)]
        public string? SortBy { get; init; } = null;

        /// <summary>
        /// "asc" or "desc"; used with <see cref="SortBy"/>.
        /// </summary>
        public string SortDirection { get; init; } = "desc";
        public Dictionary<string, string> Filters { get; init; } = [];
    }
}
