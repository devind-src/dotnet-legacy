using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.CardHotcards;

namespace SyncNetApi.Services.CardHotcards
{
    public interface ICardHotcardService
    {
        Task<IReadOnlyList<CardHotcardDto>> GetRecordsAsync(string? filter = null);
        Task<CardHotcardDto?> GetByIdAsync(string cardNr);
        Task<CardHotcardDto> CreateAsync(CreateCardHotcardRequest request, string actingUser);
        Task<CardHotcardDto> UpdateAsync(string cardNr, UpdateCardHotcardRequest request, string actingUser);
        Task DeleteAsync(string cardNr, string actingUser);
    }
}
