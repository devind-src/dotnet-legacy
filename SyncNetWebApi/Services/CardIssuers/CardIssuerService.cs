using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.CardIssuers;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.CardIssuers
{
    public class CardIssuerService : ICardIssuerService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public CardIssuerService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<CardIssuerDto>> GetRecordsAsync(string? filter = null)
        {
            var query =
                from i in _context.CardIssuers.AsNoTracking()
                join c in _context.CardIssuerContacts.AsNoTracking() on i.issuer equals c.issuer into contacts
                from c in contacts.DefaultIfEmpty()
                join k in _context.CardIssuerKeys.AsNoTracking() on i.issuer equals k.issuer into keys
                from k in keys.DefaultIfEmpty()
                select new { Issuer = i, Contact = c, Key = k };

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x => x.Issuer.issuer.ToLower().Contains(f) || (x.Issuer.inst_id ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(x => x.Issuer.issuer).ToListAsync();
            return list.Select(x => ToDto(x.Issuer, x.Contact, x.Key)).ToList();
        }

        public async Task<CardIssuerDto?> GetByIdAsync(string issuer)
        {
            var entity = await _context.CardIssuers.AsNoTracking().FirstOrDefaultAsync(i => i.issuer == issuer);
            if (entity == null) return null;

            var contact = await _context.CardIssuerContacts.AsNoTracking().FirstOrDefaultAsync(c => c.issuer == issuer);
            var key = await _context.CardIssuerKeys.AsNoTracking().FirstOrDefaultAsync(k => k.issuer == issuer);
            return ToDto(entity, contact, key);
        }

        public async Task<CardIssuerDto> CreateAsync(CreateCardIssuerRequest request, string actingUser)
        {
            if (await _context.CardIssuers.AsNoTracking().AnyAsync(i => i.issuer == request.Issuer))
                throw new ConflictException($"Issuer '{request.Issuer}' already exists.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var issuer = new CmsIssuer
                {
                    issuer = request.Issuer,
                    inst_id = request.InstId,
                    currency = request.Currency,
                    auth_service = request.AuthService ? "1" : "0",
                    velocity_per_tran = request.VelocityPerTran ? "1" : "0",
                    velocity_daily = request.VelocityDaily ? "1" : "0",
                    velocity_weekly = request.VelocityWeekly ? "1" : "0",
                    velocity_monthly = request.VelocityMonthly ? "1" : "0",
                    max_pin_tries = request.MaxPinTries,
                    last_update = DateTime.UtcNow,
                    update_by = actingUser
                };
                _context.CardIssuers.Add(issuer);
                _audit.LogInsert(issuer, "cms_issuers", actingUser);
                await _context.SaveChangesAsync();

                var contact = new CmsIssuerContact
                {
                    issuer = request.Issuer,
                    contact_name = request.ContactName,
                    phone = request.Phone,
                    fax = request.Fax,
                    mobile = request.Mobile,
                    email = request.Email,
                    address_1 = request.BusinessAddress,
                    city_1 = request.City,
                    postal_1 = request.PostalCode,
                    last_update = DateTime.UtcNow,
                    update_by = actingUser
                };
                _context.CardIssuerContacts.Add(contact);
                _audit.LogInsert(contact, "cms_issuer_contacts", actingUser);

                var key = new CmsIssuerKey
                {
                    issuer = request.Issuer,
                    key_length = request.KeyLength,
                    pinblock_format = request.PinblockFormat,
                    master_key = request.MasterKey,
                    master_key_kcv = request.MasterKcv,
                    key_under_lmk = request.KeyUnderLmk,
                    key_under_zmk = request.KeyUnderZmk,
                    key_check_value = request.KeyCheckValue
                };
                _context.CardIssuerKeys.Add(key);
                _audit.LogInsert(key, "cms_issuer_keys", actingUser);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ToDto(issuer, contact, key);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<CardIssuerDto> UpdateAsync(string issuer, UpdateCardIssuerRequest request, string actingUser)
        {
            var entity = await _context.CardIssuers.FirstOrDefaultAsync(i => i.issuer == issuer)
                ?? throw new NotFoundException($"Issuer '{issuer}' not found.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var beforeIssuer = new { entity.inst_id, entity.currency, entity.auth_service, entity.velocity_per_tran, entity.velocity_daily, entity.velocity_weekly, entity.velocity_monthly, entity.max_pin_tries };
                entity.inst_id = request.InstId;
                entity.currency = request.Currency;
                entity.auth_service = request.AuthService ? "1" : "0";
                entity.velocity_per_tran = request.VelocityPerTran ? "1" : "0";
                entity.velocity_daily = request.VelocityDaily ? "1" : "0";
                entity.velocity_weekly = request.VelocityWeekly ? "1" : "0";
                entity.velocity_monthly = request.VelocityMonthly ? "1" : "0";
                entity.max_pin_tries = request.MaxPinTries;
                entity.last_update = DateTime.UtcNow;
                entity.update_by = actingUser;

                _audit.LogUpdate(beforeIssuer, new { entity.inst_id, entity.currency, entity.auth_service, entity.velocity_per_tran, entity.velocity_daily, entity.velocity_weekly, entity.velocity_monthly, entity.max_pin_tries }, "cms_issuers", actingUser);
                await _context.SaveChangesAsync();

                var contact = await _context.CardIssuerContacts.FirstOrDefaultAsync(c => c.issuer == issuer);
                if (contact != null)
                {
                    var beforeContact = new { contact.contact_name, contact.phone, contact.fax, contact.mobile, contact.email, contact.address_1, contact.city_1, contact.postal_1 };
                    contact.contact_name = request.ContactName;
                    contact.phone = request.Phone;
                    contact.fax = request.Fax;
                    contact.mobile = request.Mobile;
                    contact.email = request.Email;
                    contact.address_1 = request.BusinessAddress;
                    contact.city_1 = request.City;
                    contact.postal_1 = request.PostalCode;
                    contact.last_update = DateTime.UtcNow;
                    contact.update_by = actingUser;

                    _audit.LogUpdate(beforeContact, new { contact.contact_name, contact.phone, contact.fax, contact.mobile, contact.email, contact.address_1, contact.city_1, contact.postal_1 }, "cms_issuer_contacts", actingUser);
                    await _context.SaveChangesAsync();
                }

                var key = await _context.CardIssuerKeys.FirstOrDefaultAsync(k => k.issuer == issuer);
                if (key != null)
                {
                    var beforeKey = new { key.key_length, key.pinblock_format, key.master_key, key.master_key_kcv, key.key_under_lmk, key.key_under_zmk, key.key_check_value };
                    key.key_length = request.KeyLength;
                    key.pinblock_format = request.PinblockFormat;
                    key.master_key = request.MasterKey;
                    key.master_key_kcv = request.MasterKcv;
                    key.key_under_lmk = request.KeyUnderLmk;
                    key.key_under_zmk = request.KeyUnderZmk;
                    key.key_check_value = request.KeyCheckValue;

                    _audit.LogUpdate(beforeKey, new { key.key_length, key.pinblock_format, key.master_key, key.master_key_kcv, key.key_under_lmk, key.key_under_zmk, key.key_check_value }, "cms_issuer_keys", actingUser);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return ToDto(entity, contact, key);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(string issuer, string actingUser)
        {
            var entity = await _context.CardIssuers.FirstOrDefaultAsync(i => i.issuer == issuer)
                ?? throw new NotFoundException($"Issuer '{issuer}' not found.");

            if (await _context.CardBranches.AsNoTracking().AnyAsync(b => b.issuer == issuer))
                throw new ConflictException($"Issuer '{issuer}' is still referenced by one or more branches and cannot be deleted.");

            if (await _context.CardProducts.AsNoTracking().AnyAsync(p => p.issuer == issuer))
                throw new ConflictException($"Issuer '{issuer}' is still referenced by one or more products and cannot be deleted.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var key = await _context.CardIssuerKeys.FirstOrDefaultAsync(k => k.issuer == issuer);
                if (key != null)
                {
                    _context.CardIssuerKeys.Remove(key);
                    _audit.LogDelete(new { key.id, key.issuer }, "cms_issuer_keys", actingUser);
                    await _context.SaveChangesAsync();
                }

                var contact = await _context.CardIssuerContacts.FirstOrDefaultAsync(c => c.issuer == issuer);
                if (contact != null)
                {
                    _context.CardIssuerContacts.Remove(contact);
                    _audit.LogDelete(new { contact.id, contact.issuer }, "cms_issuer_contacts", actingUser);
                    await _context.SaveChangesAsync();
                }

                _context.CardIssuers.Remove(entity);
                _audit.LogDelete(new { entity.issuer }, "cms_issuers", actingUser);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static CardIssuerDto ToDto(CmsIssuer i, CmsIssuerContact? c, CmsIssuerKey? k) => new(
            i.issuer,
            i.inst_id,
            i.currency,
            i.auth_service == "1",
            i.velocity_per_tran == "1",
            i.velocity_daily == "1",
            i.velocity_weekly == "1",
            i.velocity_monthly == "1",
            i.max_pin_tries,
            c?.contact_name,
            c?.phone,
            c?.fax,
            c?.mobile,
            c?.email,
            c?.address_1,
            c?.city_1,
            c?.postal_1,
            k?.key_length,
            k?.pinblock_format,
            k?.master_key,
            k?.master_key_kcv,
            k?.key_under_lmk,
            k?.key_under_zmk,
            k?.key_check_value);
    }
}
