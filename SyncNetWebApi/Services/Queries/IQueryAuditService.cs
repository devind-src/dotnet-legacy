using SyncNetApi.Dtos.Common;
using SyncNetApi.Dtos.Queries;

namespace SyncNetApi.Services.Queries
{
    public interface IQueryAuditService
    {
        Task<PagedResultDto<AuditListItemDto>> SearchAsync(AuditSearchRequest request);
        Task<AuditDetailDto?> GetDetailAsync(long id);
    }
}
