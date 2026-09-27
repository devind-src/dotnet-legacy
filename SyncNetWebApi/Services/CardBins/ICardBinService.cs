using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.CardBins;

namespace SyncNetApi.Services.CardBins
{
    public interface ICardBinService
    {
        Task<IReadOnlyList<CardBinDto>> GetRecordsAsync(string? filter = null);
        Task<CardBinDto?> GetByIdAsync(string binNr);
        Task<CardBinDto> CreateAsync(CreateCardBinRequest request, string actingUser);
        Task<CardBinDto> UpdateAsync(string binNr, UpdateCardBinRequest request, string actingUser);
        Task DeleteAsync(string binNr, string actingUser);
    }
}
