using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.RouteSupplierStatuses;

namespace SyncNetApi.Services.RouteSupplierStatuses
{
    public interface IRouteSupplierStatusService
    {
        Task<IReadOnlyList<RouteSupplierStatusDto>> GetRecordsAsync(string? filter = null);
        Task<RouteSupplierStatusDto> ResetAsync(string supplierId, string actingUser);
    }
}
