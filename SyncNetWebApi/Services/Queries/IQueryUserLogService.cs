using SyncNetApi.Dtos.Common;
using SyncNetApi.Dtos.Queries;

namespace SyncNetApi.Services.Queries
{
    public interface IQueryUserLogService
    {
        Task<PagedResultDto<UserLogItemDto>> SearchAsync(UserLogSearchRequest request);
    }
}
