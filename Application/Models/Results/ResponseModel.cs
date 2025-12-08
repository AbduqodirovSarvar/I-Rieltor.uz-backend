using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Results
{
    public class ResponseModel<TEntity>
    {
        public bool Success { get; set; }
        public HttpStatusCode? StatusCode { get; set; } = HttpStatusCode.OK;
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
        public TEntity? Data { get; set; } = default;
        public int? PageIndex { get; set; } = null;
        public int? PageSize { get; set; } = null;
        public int? TotalCount { get; set; } = null;
        public bool IsPaginated => PageIndex.HasValue && PageSize.HasValue && TotalCount.HasValue;

        public static ResponseModel<TEntity> Ok(string? message = null)
        => new() { Success = true, Message = message };

        public static ResponseModel<TEntity> Ok(TEntity data, string? message = null)
            => new() { Success = true, Message = message, Data = data };

        public static ResponseModel<TEntity> Ok(TEntity data, int pageIndex, int pageSize, int totalCount, string? message = null)
            => new()
            {
                Success = true,
                Message = message,
                Data = data,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount
            };

        public static ResponseModel<TEntity> Fail(string message, List<string>? errors = null, HttpStatusCode? statusCode = null)
            => new()
            {
                Success = false,
                Message = message,
                Errors = errors,
                StatusCode = statusCode ?? HttpStatusCode.BadRequest
            };
    }
}
