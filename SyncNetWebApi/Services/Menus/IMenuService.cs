using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Menus;

namespace SyncNetApi.Services.Menus
{
    public interface IMenuService
    {
        Task<IReadOnlyList<MenuDto>> GetRecordsAsync(string? filter = null);
        Task<MenuDto?> GetByIdAsync(int menuId);
        Task<MenuDto> CreateAsync(CreateMenuRequest request, string actingUser);
        Task<MenuDto> UpdateAsync(int menuId, UpdateMenuRequest request, string actingUser);
        Task DeleteAsync(int menuId, string actingUser);
    }
}
