using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.CardProducts;

namespace SyncNetApi.Services.CardProducts
{
    public interface ICardProductService
    {
        Task<IReadOnlyList<CardProductDto>> GetRecordsAsync(string? filter = null);
        Task<CardProductDto?> GetByIdAsync(int id);
        Task<CardProductDto> CreateAsync(CreateCardProductRequest request, string actingUser);
        Task<CardProductDto> UpdateAsync(int id, UpdateCardProductRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);
    }
}
