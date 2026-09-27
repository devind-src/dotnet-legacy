using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.RouteFailoverLogs;

namespace SyncNetApi.Services.RouteFailoverLogs
{
    public interface IRouteFailoverLogService
    {
        Task<IReadOnlyList<RouteFailoverLogDto>> GetRecordsAsync(DateTime? from = null, DateTime? to = null,
            string? instId = null, string? supplierId = null, string? reason = null, string? routingType = null);
    }
}
