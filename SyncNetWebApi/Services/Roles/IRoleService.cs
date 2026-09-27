using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Roles;

namespace SyncNetApi.Services.Roles
{
    public interface IRoleService
    {
        Task<IReadOnlyList<RoleDto>> GetRecordsAsync(string? filter = null);
        Task<RoleDetailDto?> GetDetailAsync(string roleName);
        Task<RoleDetailDto> CreateAsync(CreateRoleRequest request, string actingUser);
        Task<RoleDetailDto> UpdateAsync(string roleName, UpdateRoleRequest request, string actingUser);
        Task DeleteAsync(string roleName, string actingUser);
    }
}
