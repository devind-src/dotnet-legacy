using SyncNetApi.Dtos.Common;
using SyncNetApi.Dtos.Queries;

namespace SyncNetApi.Services.Queries
{
    public interface IQueryTransactionService
    {
        Task<PagedResultDto<TransactionListItemDto>> SearchAsync(TransactionSearchRequest request);
        Task<TransactionDetailDto?> GetDetailAsync(long tranNr);
    }
}
