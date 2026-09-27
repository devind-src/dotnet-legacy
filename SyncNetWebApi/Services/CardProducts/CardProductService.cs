using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.CardProducts;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.CardProducts
{
    public class CardProductService : ICardProductService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public CardProductService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<CardProductDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.CardProducts.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(p => (p.issuer ?? "").ToLower().Contains(f) || (p.product ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(p => p.id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<CardProductDto?> GetByIdAsync(int id)
        {
            var entity = await _context.CardProducts.AsNoTracking().FirstOrDefaultAsync(p => p.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<CardProductDto> CreateAsync(CreateCardProductRequest request, string actingUser)
        {
            if (!await _context.CardIssuers.AsNoTracking().AnyAsync(i => i.issuer == request.Issuer))
                throw new ValidationException($"Issuer '{request.Issuer}' does not exist.");

            var entity = new CmsProduct
            {
                issuer = request.Issuer,
                product = request.Product,
                pan_prefix = request.PanPrefix,
                pan_length = request.PanLength,
                card_type = request.CardType
            };

            _context.CardProducts.Add(entity);
            _audit.LogInsert(entity, "cms_products", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<CardProductDto> UpdateAsync(int id, UpdateCardProductRequest request, string actingUser)
        {
            var entity = await _context.CardProducts.FirstOrDefaultAsync(p => p.id == id)
                ?? throw new NotFoundException($"Product '{id}' not found.");

            if (!await _context.CardIssuers.AsNoTracking().AnyAsync(i => i.issuer == request.Issuer))
                throw new ValidationException($"Issuer '{request.Issuer}' does not exist.");

            var before = new { entity.issuer, entity.product, entity.pan_prefix, entity.pan_length, entity.card_type };
            entity.issuer = request.Issuer;
            entity.product = request.Product;
            entity.pan_prefix = request.PanPrefix;
            entity.pan_length = request.PanLength;
            entity.card_type = request.CardType;

            _audit.LogUpdate(before, new { entity.issuer, entity.product, entity.pan_prefix, entity.pan_length, entity.card_type }, "cms_products", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(int id, string actingUser)
        {
            var entity = await _context.CardProducts.FirstOrDefaultAsync(p => p.id == id)
                ?? throw new NotFoundException($"Product '{id}' not found.");

            // cms_products_limit.id_product -> cms_products.id is ON DELETE CASCADE — Postgres
            // removes child Limit rows automatically, no app-level cascade needed.
            _context.CardProducts.Remove(entity);
            _audit.LogDelete(new { entity.id, entity.issuer, entity.product }, "cms_products", actingUser);

            await _context.SaveChangesAsync();
        }

        private static CardProductDto ToDto(CmsProduct e) => new(
            e.id, e.issuer, e.product, e.pan_prefix, e.pan_length, e.card_type, e.profile_name, e.last_seq_nr);
    }
}
