using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.CardBranches;

namespace SyncNetApi.Services.CardBranches
{
    public interface ICardBranchService
    {
        Task<IReadOnlyList<CardBranchDto>> GetByIssuerAsync(string issuer);
        Task<CardBranchDto?> GetByIdAsync(int id);
        Task<CardBranchDto> CreateAsync(CreateCardBranchRequest request, string actingUser);
        Task<CardBranchDto> UpdateAsync(int id, UpdateCardBranchRequest request, string actingUser);
        Task DeleteAsync(int id, string actingUser);
    }
}
