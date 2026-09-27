using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.ProductPostpaidFees;

namespace SyncNetApi.Services.ProductPostpaidFees
{
    public interface IProductPostpaidFeeService
    {
        Task<IReadOnlyList<ProductPostpaidFeeDto>> GetRecordsAsync(string? filter = null);
        Task<ProductPostpaidFeeDto?> GetByIdAsync(long id);
        Task<ProductPostpaidFeeDto> CreateAsync(CreateProductPostpaidFeeRequest request, string actingUser);
        Task<ProductPostpaidFeeDto> UpdateAsync(long id, UpdateProductPostpaidFeeRequest request, string actingUser);
        /// <summary>deleteBillers: bila ini baris fee terakhir produk, biller produk (primary + alternate) ikut dihapus.</summary>
        Task DeleteAsync(long id, string actingUser, bool deleteBillers = false);
    }
}
