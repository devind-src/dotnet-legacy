using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.MerchantCriteria;

namespace SyncNetApi.Services.MerchantCriteria
{
    public interface IMerchantCriteriaService
    {
        Task<IReadOnlyList<MerchantCriteriaDto>> GetRecordsAsync(string? filter = null);
        Task<MerchantCriteriaDto?> GetByIdAsync(long id);
        Task<MerchantCriteriaDto> CreateAsync(CreateMerchantCriteriaRequest request, string actingUser);
        Task<MerchantCriteriaDto> UpdateAsync(long id, UpdateMerchantCriteriaRequest request, string actingUser);
        Task DeleteAsync(long id, string actingUser);
    }
}
