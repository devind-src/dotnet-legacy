using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;

namespace SyncNetApi.Services.RouteRules
{
    /// <summary>Aturan bersama Routing &gt; Margin (hanya produk kategori topup) dan Routing &gt;
    /// Product (hanya produk kategori bill payment), serta larangan failover bila produk punya
    /// rule di sw_routes_by_source (push route mengalahkan route by source).</summary>
    public static class RouteCategoryRules
    {
        /// <summary>null = produk tidak ditemukan. Produk tanpa category dianggap bukan topup.</summary>
        public static async Task<bool?> IsTopupAsync(SyncNetDbContext context, string instId)
        {
            var product = await context.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.product_code == instId);
            if (product == null) return null;

            var category = await context.ProductCategories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.category == product.category);

            return category?.is_topup == "1";
        }

        public static async Task EnsureTopupAsync(SyncNetDbContext context, string instId)
        {
            var topup = await IsTopupAsync(context, instId)
                ?? throw new ValidationException($"Produk '{instId}' tidak ditemukan.");

            if (!topup)
                throw new ValidationException(
                    $"Produk '{instId}' bukan kategori topup. Routing > Margin hanya untuk produk topup.");
        }

        public static async Task EnsureBillPaymentAsync(SyncNetDbContext context, string instId)
        {
            var topup = await IsTopupAsync(context, instId)
                ?? throw new ValidationException($"Produk '{instId}' tidak ditemukan.");

            if (topup)
                throw new ValidationException(
                    $"Produk '{instId}' kategori topup. Routing > Product hanya untuk produk bill payment, gunakan Routing > Margin.");
        }

        public static Task<bool> HasSourceRuleAsync(SyncNetDbContext context, string instId)
        {
            return context.RoutesBySource.AsNoTracking().AnyAsync(r => r.inst_id == instId);
        }

        public static async Task EnsureNoSourceRuleAsync(SyncNetDbContext context, string instId)
        {
            if (await HasSourceRuleAsync(context, instId))
                throw new ValidationException(
                    $"Produk '{instId}' punya rule di Routing > Source. Mode dynamic tidak bisa dipakai karena push route mengalahkan route by source.");
        }
    }
}
