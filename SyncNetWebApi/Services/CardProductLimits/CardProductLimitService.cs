using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.CardProductLimits;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.CardProductLimits
{
    public class CardProductLimitService : ICardProductLimitService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public CardProductLimitService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<CardProductLimitDto>> GetByProductIdAsync(int productId)
        {
            var list = await _context.CardProductLimits.AsNoTracking()
                .Where(l => l.id_product == productId)
                .OrderBy(l => l.channel)
                .ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<CardProductLimitDto?> GetByIdAsync(int id)
        {
            var entity = await _context.CardProductLimits.AsNoTracking().FirstOrDefaultAsync(l => l.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<CardProductLimitDto> CreateAsync(CreateCardProductLimitRequest request, string actingUser)
        {
            if (!await _context.CardProducts.AsNoTracking().AnyAsync(p => p.id == request.ProductId))
                throw new ValidationException($"Product '{request.ProductId}' does not exist.");

            var entity = new CmsProductLimit
            {
                id_product = request.ProductId,
                channel = request.Channel,
                amt_purchase_per_tran = request.AmtPurchasePerTran,
                amt_cash_per_tran = request.AmtCashPerTran,
                amt_payment_per_tran = request.AmtPaymentPerTran,
                amt_transfer_per_tran = request.AmtTransferPerTran,
                nr_inquiry_daily = request.NrInquiryDaily,
                nr_purchase_daily = request.NrPurchaseDaily,
                nr_cash_daily = request.NrCashDaily,
                nr_payment_daily = request.NrPaymentDaily,
                nr_transfer_daily = request.NrTransferDaily,
                amt_purchase_daily = request.AmtPurchaseDaily,
                amt_cash_daily = request.AmtCashDaily,
                amt_payment_daily = request.AmtPaymentDaily,
                amt_transfer_daily = request.AmtTransferDaily,
                nr_inquiry_weekly = request.NrInquiryWeekly,
                nr_purchase_weekly = request.NrPurchaseWeekly,
                nr_cash_weekly = request.NrCashWeekly,
                nr_payment_weekly = request.NrPaymentWeekly,
                nr_transfer_weekly = request.NrTransferWeekly,
                amt_purchase_weekly = request.AmtPurchaseWeekly,
                amt_cash_weekly = request.AmtCashWeekly,
                amt_payment_weekly = request.AmtPaymentWeekly,
                amt_transfer_weekly = request.AmtTransferWeekly,
                nr_inquiry_monthly = request.NrInquiryMonthly,
                nr_purchase_monthly = request.NrPurchaseMonthly,
                nr_cash_monthly = request.NrCashMonthly,
                nr_payment_monthly = request.NrPaymentMonthly,
                nr_transfer_monthly = request.NrTransferMonthly,
                amt_purchase_monthly = request.AmtPurchaseMonthly,
                amt_cash_monthly = request.AmtCashMonthly,
                amt_payment_monthly = request.AmtPaymentMonthly,
                amt_transfer_monthly = request.AmtTransferMonthly,
                // last_update/update_by are always server-controlled — matches legacy, never
                // taken from client input.
                last_update = DateTime.UtcNow,
                update_by = actingUser
            };

            _context.CardProductLimits.Add(entity);
            _audit.LogInsert(entity, "cms_products_limit", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<CardProductLimitDto> UpdateAsync(int id, UpdateCardProductLimitRequest request, string actingUser)
        {
            var entity = await _context.CardProductLimits.FirstOrDefaultAsync(l => l.id == id)
                ?? throw new NotFoundException($"Product limit '{id}' not found.");

            var before = ToDto(entity);

            entity.channel = request.Channel;
            entity.amt_purchase_per_tran = request.AmtPurchasePerTran;
            entity.amt_cash_per_tran = request.AmtCashPerTran;
            entity.amt_payment_per_tran = request.AmtPaymentPerTran;
            entity.amt_transfer_per_tran = request.AmtTransferPerTran;
            entity.nr_inquiry_daily = request.NrInquiryDaily;
            entity.nr_purchase_daily = request.NrPurchaseDaily;
            entity.nr_cash_daily = request.NrCashDaily;
            entity.nr_payment_daily = request.NrPaymentDaily;
            entity.nr_transfer_daily = request.NrTransferDaily;
            entity.amt_purchase_daily = request.AmtPurchaseDaily;
            entity.amt_cash_daily = request.AmtCashDaily;
            entity.amt_payment_daily = request.AmtPaymentDaily;
            entity.amt_transfer_daily = request.AmtTransferDaily;
            entity.nr_inquiry_weekly = request.NrInquiryWeekly;
            entity.nr_purchase_weekly = request.NrPurchaseWeekly;
            entity.nr_cash_weekly = request.NrCashWeekly;
            entity.nr_payment_weekly = request.NrPaymentWeekly;
            entity.nr_transfer_weekly = request.NrTransferWeekly;
            entity.amt_purchase_weekly = request.AmtPurchaseWeekly;
            entity.amt_cash_weekly = request.AmtCashWeekly;
            entity.amt_payment_weekly = request.AmtPaymentWeekly;
            entity.amt_transfer_weekly = request.AmtTransferWeekly;
            entity.nr_inquiry_monthly = request.NrInquiryMonthly;
            entity.nr_purchase_monthly = request.NrPurchaseMonthly;
            entity.nr_cash_monthly = request.NrCashMonthly;
            entity.nr_payment_monthly = request.NrPaymentMonthly;
            entity.nr_transfer_monthly = request.NrTransferMonthly;
            entity.amt_purchase_monthly = request.AmtPurchaseMonthly;
            entity.amt_cash_monthly = request.AmtCashMonthly;
            entity.amt_payment_monthly = request.AmtPaymentMonthly;
            entity.amt_transfer_monthly = request.AmtTransferMonthly;
            entity.last_update = DateTime.UtcNow;
            entity.update_by = actingUser;

            await _context.SaveChangesAsync();
            var after = ToDto(entity);
            _audit.LogUpdate(before, after, "cms_products_limit", actingUser);

            return after;
        }

        public async Task DeleteAsync(int id, string actingUser)
        {
            var entity = await _context.CardProductLimits.FirstOrDefaultAsync(l => l.id == id)
                ?? throw new NotFoundException($"Product limit '{id}' not found.");

            _context.CardProductLimits.Remove(entity);
            _audit.LogDelete(new { entity.id, entity.id_product, entity.channel }, "cms_products_limit", actingUser);

            await _context.SaveChangesAsync();
        }

        private static CardProductLimitDto ToDto(CmsProductLimit e) => new(
            e.id, e.id_product ?? 0, e.channel,
            e.amt_purchase_per_tran, e.amt_cash_per_tran, e.amt_payment_per_tran, e.amt_transfer_per_tran,
            e.nr_inquiry_daily, e.nr_purchase_daily, e.nr_cash_daily, e.nr_payment_daily, e.nr_transfer_daily,
            e.amt_purchase_daily, e.amt_cash_daily, e.amt_payment_daily, e.amt_transfer_daily,
            e.nr_inquiry_weekly, e.nr_purchase_weekly, e.nr_cash_weekly, e.nr_payment_weekly, e.nr_transfer_weekly,
            e.amt_purchase_weekly, e.amt_cash_weekly, e.amt_payment_weekly, e.amt_transfer_weekly,
            e.nr_inquiry_monthly, e.nr_purchase_monthly, e.nr_cash_monthly, e.nr_payment_monthly, e.nr_transfer_monthly,
            e.amt_purchase_monthly, e.amt_cash_monthly, e.amt_payment_monthly, e.amt_transfer_monthly);
    }
}
