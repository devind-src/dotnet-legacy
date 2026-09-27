using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.CardIssuers;

namespace SyncNetApi.Services.CardIssuers
{
    public interface ICardIssuerService
    {
        Task<IReadOnlyList<CardIssuerDto>> GetRecordsAsync(string? filter = null);
        Task<CardIssuerDto?> GetByIdAsync(string issuer);
        Task<CardIssuerDto> CreateAsync(CreateCardIssuerRequest request, string actingUser);
        Task<CardIssuerDto> UpdateAsync(string issuer, UpdateCardIssuerRequest request, string actingUser);
        Task DeleteAsync(string issuer, string actingUser);
    }
}
