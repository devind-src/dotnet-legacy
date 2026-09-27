using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.JobFees;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.JobFees
{
    public class JobFeeService : IJobFeeService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public JobFeeService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<JobFeeDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.JobFees.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(j => (j.job_desc ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderByDescending(j => j.id).ToListAsync();
            var counts = await _context.JobFeeDetails.AsNoTracking()
                .GroupBy(d => d.job_id)
                .Select(g => new { JobId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.JobId, x => x.Count);

            return list.Select(e => ToDto(e, counts.TryGetValue((int)e.id, out var c) ? c : 0)).ToList();
        }

        public async Task<JobFeeDto?> GetByIdAsync(long id)
        {
            var entity = await _context.JobFees.AsNoTracking().FirstOrDefaultAsync(j => j.id == id);
            if (entity == null) return null;

            var count = await _context.JobFeeDetails.AsNoTracking().CountAsync(d => d.job_id == (int)id);
            return ToDto(entity, count);
        }

        public async Task<IReadOnlyList<JobFeeDetailDto>> GetDetailsAsync(long id)
        {
            var details = await _context.JobFeeDetails.AsNoTracking()
                .Where(d => d.job_id == (int)id)
                .OrderBy(d => d.id)
                .ToListAsync();

            return details.Select(ToDetailDto).ToList();
        }

        public async Task<JobFeeDto> CreateAsync(string jobDesc, string? scheduledAt, IFormFile file, string actingUser)
        {
            if (string.IsNullOrWhiteSpace(jobDesc))
                throw new ValidationException("Job desc mandatory.");
            if (file == null || file.Length == 0)
                throw new ValidationException("File CSV mandatory.");

            var rows = await ParseCsvAsync(file);
            if (rows.Count == 0)
                throw new ValidationException("Data fee mandatory, tidak ada baris valid pada file CSV.");

            foreach (var row in rows)
            {
                var (isValid, error, _) = await ValidateRowAsync(row);
                if (!isValid)
                    throw new ValidationException(error!);
            }

            DateTime? scheduledDt = null;
            if (!string.IsNullOrWhiteSpace(scheduledAt) && DateTime.TryParse(scheduledAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                scheduledDt = parsed;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var master = new SwJobFee
                {
                    job_desc = jobDesc,
                    job_status = "0",
                    scheduled_at = scheduledDt,
                    created_at = DateTime.UtcNow
                };
                _context.JobFees.Add(master);
                await _context.SaveChangesAsync();

                var details = rows.Select(r => new SwJobFeeDetail
                {
                    job_id = (int)master.id,
                    product_id = r.ProductId,
                    group_name = r.GroupName,
                    subgroup_name = r.SubgroupName,
                    merchant_id = r.MerchantId,
                    submerchant_id = r.SubmerchantId,
                    fee_type = r.FeeType,
                    fixed_fee = r.FeeType == "0" ? RoundToInt(r.FeeTotal) : 0,
                    fixed_fee_acq = r.FeeType == "0" ? RoundToInt(r.FeeAcq) : 0,
                    fixed_fee_iss = r.FeeType == "0" ? RoundToInt(r.FeeIss) : 0,
                    fixed_fee_swt = r.FeeType == "0" ? RoundToInt(r.FeeSwt) : 0,
                    percent_fee = r.FeeType == "1" ? r.FeeTotal : 0,
                    percent_fee_acq = r.FeeType == "1" ? r.FeeAcq : 0,
                    percent_fee_iss = r.FeeType == "1" ? r.FeeIss : 0,
                    percent_fee_swt = r.FeeType == "1" ? r.FeeSwt : 0
                }).ToList();

                _context.JobFeeDetails.AddRange(details);

                _audit.LogInsert(master, "sw_job_fee", actingUser);
                _audit.LogInsert(details, "sw_job_fee_detail", actingUser);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ToDto(master, details.Count);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<JobFeePreviewResultDto> PreviewAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ValidationException("File CSV mandatory.");

            var rows = await ParseCsvAsync(file);
            var previewRows = new List<JobFeePreviewRowDto>();

            var rowNumber = 0;
            foreach (var row in rows)
            {
                rowNumber++;
                var (isValid, error, productName) = await ValidateRowAsync(row);
                previewRows.Add(new JobFeePreviewRowDto(
                    rowNumber, row.ProductId, productName, row.GroupName, row.SubgroupName,
                    row.MerchantId, row.SubmerchantId, row.FeeType == "1" ? "Percent" : "Fixed",
                    row.FeeTotal, row.FeeAcq, row.FeeIss, row.FeeSwt, isValid, error));
            }

            var validCount = previewRows.Count(r => r.IsValid);
            return new JobFeePreviewResultDto(previewRows.Count, validCount, previewRows.Count - validCount, previewRows);
        }

        /// <summary>Shared per-row validation for Create and Preview — mirrors legacy
        /// HandleFileSelected: product must exist in Product Master, merchant (if given) must
        /// be 14/15 digits and exist in sw_merchant. Submerchant is intentionally not
        /// validated (legacy doesn't validate it either, only resolves its name for display).</summary>
        private async Task<(bool IsValid, string? Error, string? ProductName)> ValidateRowAsync(ParsedFeeRow row)
        {
            if (string.IsNullOrWhiteSpace(row.ProductId) || !await _context.Products.AsNoTracking().AnyAsync(p => p.product_code == row.ProductId))
                return (false, $"Product ID '{row.ProductId}' belum terdaftar.", null);

            var productName = await _context.Products.AsNoTracking()
                .Where(p => p.product_code == row.ProductId)
                .Select(p => p.product_name)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(row.MerchantId))
            {
                if (row.MerchantId.Length != 14 && row.MerchantId.Length != 15)
                    return (false, $"Panjang merchant ID harus 14/15 digit, value '{row.MerchantId}'.", productName);

                if (!await _context.Merchants.AsNoTracking().AnyAsync(m => m.merchant_id == row.MerchantId))
                    return (false, $"Merchant ID '{row.MerchantId}' belum terdaftar.", productName);
            }

            return (true, null, productName);
        }

        public async Task DeleteAsync(long id, string actingUser)
        {
            var entity = await _context.JobFees.FirstOrDefaultAsync(j => j.id == id)
                ?? throw new NotFoundException($"Job fee '{id}' not found.");

            var details = await _context.JobFeeDetails.Where(d => d.job_id == (int)id).ToListAsync();
            if (details.Count > 0)
            {
                _context.JobFeeDetails.RemoveRange(details);
                _audit.LogDelete(new { job_id = id, detail_count = details.Count }, "sw_job_fee_detail", actingUser);
            }

            _context.JobFees.Remove(entity);
            _audit.LogDelete(new { entity.id, entity.job_desc }, "sw_job_fee", actingUser);

            await _context.SaveChangesAsync();
        }

        private static async Task<List<ParsedFeeRow>> ParseCsvAsync(IFormFile file)
        {
            var rows = new List<ParsedFeeRow>();

            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Skip header row (row 0), matches legacy NbImport.ImportFees.
            for (var i = 1; i < lines.Length; i++)
            {
                var fields = lines[i].Split(';');
                if (fields.Length < 10) continue;

                var feeTypeRaw = fields[5].Trim('"').Trim();
                string feeTypeCode;
                if (string.Equals(feeTypeRaw, "fixed", StringComparison.OrdinalIgnoreCase)) feeTypeCode = "0";
                else if (string.Equals(feeTypeRaw, "percent", StringComparison.OrdinalIgnoreCase)) feeTypeCode = "1";
                else continue;

                rows.Add(new ParsedFeeRow(
                    fields[0].Trim('"').Trim(),
                    fields[1].Trim('"').Trim(),
                    fields[2].Trim('"').Trim(),
                    fields[3].Trim('"').Trim(),
                    fields[4].Trim('"').Trim(),
                    feeTypeCode,
                    ToDecimalOrZero(fields[6]),
                    ToDecimalOrZero(fields[7]),
                    ToDecimalOrZero(fields[8]),
                    ToDecimalOrZero(fields[9])));
            }

            return rows;
        }

        private static decimal ToDecimalOrZero(string raw)
        {
            var value = raw.Trim('"').Trim();
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;
        }

        private static int RoundToInt(decimal value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

        private static JobFeeDto ToDto(SwJobFee e, int detailCount) =>
            new(e.id, e.job_desc, e.job_status == "1", e.scheduled_at, e.created_at, detailCount);

        private static JobFeeDetailDto ToDetailDto(SwJobFeeDetail e) => new(
            e.id, e.product_id, e.group_name, e.subgroup_name, e.merchant_id, e.submerchant_id,
            e.fee_type == "1" ? "Percent" : "Fixed",
            e.fixed_fee, e.fixed_fee_acq, e.fixed_fee_iss, e.fixed_fee_swt,
            e.percent_fee, e.percent_fee_acq, e.percent_fee_iss, e.percent_fee_swt);

        private record ParsedFeeRow(
            string ProductId, string GroupName, string SubgroupName, string MerchantId, string SubmerchantId,
            string FeeType, decimal FeeTotal, decimal FeeAcq, decimal FeeIss, decimal FeeSwt);
    }
}
