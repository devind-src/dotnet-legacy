using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.CardBranches;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.CardBranches
{
    public class CardBranchService : ICardBranchService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public CardBranchService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<CardBranchDto>> GetByIssuerAsync(string issuer)
        {
            var list = await _context.CardBranches.AsNoTracking()
                .Where(b => b.issuer == issuer)
                .OrderBy(b => b.branch)
                .ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<CardBranchDto?> GetByIdAsync(int id)
        {
            var entity = await _context.CardBranches.AsNoTracking().FirstOrDefaultAsync(b => b.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<CardBranchDto> CreateAsync(CreateCardBranchRequest request, string actingUser)
        {
            if (!await _context.CardIssuers.AsNoTracking().AnyAsync(i => i.issuer == request.Issuer))
                throw new ValidationException($"Issuer '{request.Issuer}' does not exist.");

            var entity = new CmsBranch
            {
                issuer = request.Issuer,
                idbranch = request.IdBranch,
                branch = request.Branch,
                address = request.Address,
                city = request.City,
                phone = request.Phone,
                fax = request.Fax,
                email = request.Email,
                contact = request.Contact
            };

            _context.CardBranches.Add(entity);
            _audit.LogInsert(entity, "cms_branch", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<CardBranchDto> UpdateAsync(int id, UpdateCardBranchRequest request, string actingUser)
        {
            var entity = await _context.CardBranches.FirstOrDefaultAsync(b => b.id == id)
                ?? throw new NotFoundException($"Branch '{id}' not found.");

            var before = new { entity.idbranch, entity.branch, entity.address, entity.city, entity.phone, entity.fax, entity.email, entity.contact };
            entity.idbranch = request.IdBranch;
            entity.branch = request.Branch;
            entity.address = request.Address;
            entity.city = request.City;
            entity.phone = request.Phone;
            entity.fax = request.Fax;
            entity.email = request.Email;
            entity.contact = request.Contact;

            _audit.LogUpdate(before, new { entity.idbranch, entity.branch, entity.address, entity.city, entity.phone, entity.fax, entity.email, entity.contact }, "cms_branch", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(int id, string actingUser)
        {
            var entity = await _context.CardBranches.FirstOrDefaultAsync(b => b.id == id)
                ?? throw new NotFoundException($"Branch '{id}' not found.");

            _context.CardBranches.Remove(entity);
            _audit.LogDelete(new { entity.id, entity.issuer, entity.idbranch }, "cms_branch", actingUser);

            await _context.SaveChangesAsync();
        }

        private static CardBranchDto ToDto(CmsBranch e) => new(
            e.id, e.issuer ?? string.Empty, e.idbranch, e.branch, e.address, e.city, e.phone, e.fax, e.email, e.contact);
    }
}
