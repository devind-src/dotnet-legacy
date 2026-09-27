using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.MerchantCriteria;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.MerchantCriteria
{
    public class MerchantCriteriaService : IMerchantCriteriaService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public MerchantCriteriaService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<MerchantCriteriaDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.MerchantCriteria.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x => x.criteria.ToLower().Contains(f)
                    || x.group_name.ToLower().Contains(f)
                    || x.subgroup_name.ToLower().Contains(f)
                    || x.notes.ToLower().Contains(f));
            }

            var list = await query.OrderBy(x => x.criteria).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<MerchantCriteriaDto?> GetByIdAsync(long id)
        {
            var entity = await _context.MerchantCriteria.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<MerchantCriteriaDto> CreateAsync(CreateMerchantCriteriaRequest request, string actingUser)
        {
            var (groupName, subgroupName) = NormalizeGroups(request.GroupName, request.SubgroupName);
            ValidateFeeShare(request.FeeToMerchant, request.FeeToSubmerchant, request.FeeToSwitch);

            if (await _context.MerchantCriteria.AsNoTracking()
                    .AnyAsync(x => x.criteria == request.Criteria && x.group_name == groupName && x.subgroup_name == subgroupName))
                throw new ConflictException("Kombinasi data criteria, group dan sub group harus unik.");

            // id is GENERATED ALWAYS AS IDENTITY — do not assign manually (see entity note).
            var entity = new SwMerchantCriteria
            {
                criteria = request.Criteria,
                mdr = request.Mdr,
                fee_from_issuer = request.FeeFromIssuer,
                fee_from_partner = request.FeeFromPartner,
                fee_to_merchant = request.FeeToMerchant,
                fee_to_submerchant = request.FeeToSubmerchant,
                fee_to_switch = request.FeeToSwitch,
                notes = request.Notes,
                group_name = groupName,
                subgroup_name = subgroupName
            };

            _context.MerchantCriteria.Add(entity);
            await _context.SaveChangesAsync();

            _audit.LogInsert(entity, "sw_merchant_criteria", actingUser);
            await _context.SaveChangesAsync();

            return ToDto(entity);
        }

        public async Task<MerchantCriteriaDto> UpdateAsync(long id, UpdateMerchantCriteriaRequest request, string actingUser)
        {
            var entity = await _context.MerchantCriteria.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Merchant criteria '{id}' not found.");

            var (groupName, subgroupName) = NormalizeGroups(request.GroupName, request.SubgroupName);
            ValidateFeeShare(request.FeeToMerchant, request.FeeToSubmerchant, request.FeeToSwitch);

            if (await _context.MerchantCriteria.AsNoTracking()
                    .AnyAsync(x => x.id != id && x.criteria == entity.criteria && x.group_name == groupName && x.subgroup_name == subgroupName))
                throw new ConflictException("Kombinasi data criteria, group dan sub group harus unik.");

            var before = ToDto(entity);
            entity.mdr = request.Mdr;
            entity.fee_from_issuer = request.FeeFromIssuer;
            entity.fee_from_partner = request.FeeFromPartner;
            entity.fee_to_merchant = request.FeeToMerchant;
            entity.fee_to_submerchant = request.FeeToSubmerchant;
            entity.fee_to_switch = request.FeeToSwitch;
            entity.notes = request.Notes;
            entity.group_name = groupName;
            entity.subgroup_name = subgroupName;

            _audit.LogUpdate(before, ToDto(entity), "sw_merchant_criteria", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(long id, string actingUser)
        {
            var entity = await _context.MerchantCriteria.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Merchant criteria '{id}' not found.");

            _context.MerchantCriteria.Remove(entity);
            _audit.LogDelete(entity, "sw_merchant_criteria", actingUser);

            await _context.SaveChangesAsync();
        }

        // Mirrors legacy Save(): "jika sub group diisi, maka group di set kosong" — mutually
        // exclusive, subgroup wins.
        private static (string GroupName, string SubgroupName) NormalizeGroups(string groupName, string subgroupName)
        {
            groupName ??= string.Empty;
            subgroupName ??= string.Empty;
            return string.IsNullOrEmpty(subgroupName) ? (groupName, subgroupName) : (string.Empty, subgroupName);
        }

        // Mirrors legacy IsValid(): total_fee_share > 0 && != 100 => error.
        private static void ValidateFeeShare(decimal feeToMerchant, decimal feeToSubmerchant, decimal feeToSwitch)
        {
            var totalFeeShare = feeToMerchant + feeToSubmerchant + feeToSwitch;
            if (totalFeeShare > 0 && totalFeeShare != 100)
                throw new ValidationException("Jumlah fee merchant, fee sub merchant & fee switch harus 100%.");
        }

        private static MerchantCriteriaDto ToDto(SwMerchantCriteria e) => new(e.id, e.criteria, e.mdr, e.fee_from_issuer, e.fee_from_partner, e.fee_to_merchant, e.fee_to_submerchant, e.fee_to_switch, e.notes, e.group_name, e.subgroup_name);
    }
}
