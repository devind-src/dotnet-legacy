using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.RouteSchedules;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.RouteSchedules
{
    /// <summary>Routing &gt; Jadwal Routing (Fase 3, DOCS/Routing/03-routing-dinamis/FASE-3-PRIORITY-TIME.md).
    /// Setiap maintenance = aturan baru; perubahan, pembatalan, dan penghapusan menyimpan nilai lama di
    /// sw_routes_schedule_hist. Aturan Selesai/Dibatalkan read-only; aturan Sekali yang Berlangsung hanya
    /// boleh digeser jam selesainya; hapus hanya untuk aturan Akan datang (keputusan R6).</summary>
    public class RouteScheduleService : IRouteScheduleService
    {
        private const string Table = "sw_routes_schedule";

        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;
        private readonly Func<DateTime> _clock;

        public RouteScheduleService(SyncNetDbContext context, IAuditService audit)
            : this(context, audit, () => LocalClock.Now)
        {
        }

        public RouteScheduleService(SyncNetDbContext context, IAuditService audit, Func<DateTime> clock)
        {
            _context = context;
            _audit = audit;
            _clock = clock;
        }

        // detik dibuang: jadwal dihitung per menit
        private DateTime Now => TruncateToMinute(_clock());

        // ---------------------------------------------------------------- baca

        public async Task<IReadOnlyList<RouteScheduleDto>> GetRecordsAsync(string? filter = null,
            IReadOnlyCollection<string>? states = null, int? nodeId = null, string? ruleType = null)
        {
            var query = _context.RoutesSchedule.AsNoTracking().AsQueryable();
            if (nodeId.HasValue) query = query.Where(r => r.node_id == nodeId);
            if (!string.IsNullOrWhiteSpace(ruleType)) query = query.Where(r => r.rule_type == ruleType);

            var rows = await query.ToListAsync();
            var names = await LoadNamesAsync();
            var now = Now;

            var list = rows.Select(r => ToDto(r, names, now)).ToList();

            if (states is { Count: > 0 })
                list = list.Where(d => states.Contains(d.State)).ToList();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLowerInvariant();
                list = list.Where(d => d.RuleName.ToLowerInvariant().Contains(f)
                    || (d.NodeName ?? "").ToLowerInvariant().Contains(f)
                    || (d.InstId ?? "").ToLowerInvariant().Contains(f)
                    || (d.ProductName ?? "").ToLowerInvariant().Contains(f)
                    || (d.Notes ?? "").ToLowerInvariant().Contains(f)).ToList();
            }

            return list
                .OrderBy(d => StateOrder(d.State))
                .ThenBy(d => d.StartDt ?? d.CreatedDt ?? DateTime.MaxValue)
                .ThenBy(d => d.RuleName)
                .ToList();
        }

        public async Task<RouteScheduleDto?> GetByIdAsync(int id)
        {
            var e = await _context.RoutesSchedule.AsNoTracking().FirstOrDefaultAsync(r => r.id == id);
            return e == null ? null : ToDto(e, await LoadNamesAsync(), Now);
        }

        public async Task<IReadOnlyList<RouteScheduleHistDto>> GetHistoryAsync(int id)
        {
            var rows = await _context.RoutesScheduleHist.AsNoTracking()
                .Where(h => h.schedule_id == id)
                .OrderByDescending(h => h.changed_dt).ThenByDescending(h => h.hist_id)
                .ToListAsync();
            var names = await LoadNamesAsync();

            return rows.Select(h => new RouteScheduleHistDto(
                h.hist_id, h.schedule_id, h.action, h.rule_name, h.rule_type, h.routing_type, h.inst_id, h.node_id,
                h.node_id.HasValue && names.Nodes.TryGetValue(h.node_id.Value, out var n) ? n : null,
                h.priority, h.recurrence,
                ScheduleWindow.Describe(h.recurrence, h.start_dt, h.end_dt, h.time_start, h.time_end,
                    h.days_of_week, h.days_of_month, h.valid_from, h.valid_until),
                h.notes, h.status, h.cancel_reason, h.changed_by, h.changed_dt)).ToList();
        }

        // ---------------------------------------------------------------- ubah

        public async Task<RouteScheduleDto> CreateAsync(SaveRouteScheduleRequest request, string actingUser)
        {
            var entity = new SwRoutesSchedule();
            await ApplyAsync(entity, request, requireFutureEnd: true);

            var now = LocalClock.Now;
            entity.status = "1";
            entity.created_by = actingUser;
            entity.created_dt = now;
            entity.updated_by = actingUser;
            entity.updated_dt = now;

            _context.RoutesSchedule.Add(entity);
            _audit.LogInsert(entity, Table, actingUser);
            await _context.SaveChangesAsync();

            return ToDto(entity, await LoadNamesAsync(), Now);
        }

        public async Task<RouteScheduleDto> UpdateAsync(int id, SaveRouteScheduleRequest request, string actingUser)
        {
            var entity = await FindAsync(id);
            var state = ComputeState(entity, Now);
            var (canEdit, endOnly, _, _) = Permissions(entity, state.State);

            if (!canEdit && !endOnly)
                throw new ValidationException($"Aturan berstatus {RouteScheduleCodes.StateLabel(state.State)} tidak bisa diubah. Gunakan Duplikat untuk membuat aturan baru.");

            var before = Snapshot(entity);
            var draft = new SwRoutesSchedule();
            await ApplyAsync(draft, request, requireFutureEnd: true);

            if (endOnly)
            {
                // maintenance sedang berlangsung: hanya jam selesai yang boleh dimajukan/diundur
                if (!SameExceptEnd(entity, draft))
                    throw new ValidationException("Aturan sedang berlangsung: hanya Jam Selesai yang boleh diubah (maju atau mundur).");
                if (draft.end_dt < Now)
                    throw new ValidationException("Jam selesai tidak boleh sebelum waktu sekarang.");
                entity.end_dt = draft.end_dt;
            }
            else
            {
                CopyRule(draft, entity);
            }

            entity.updated_by = actingUser;
            entity.updated_dt = LocalClock.Now;

            AddHistory(before, "UPDATE", actingUser);
            _audit.LogUpdate(before, Snapshot(entity), Table, actingUser);
            await _context.SaveChangesAsync();

            return ToDto(entity, await LoadNamesAsync(), Now);
        }

        public async Task<RouteScheduleDto> CancelAsync(int id, string reason, string actingUser)
        {
            var entity = await FindAsync(id);
            var state = ComputeState(entity, Now);
            if (!Permissions(entity, state.State).CanCancel)
                throw new ValidationException($"Aturan berstatus {RouteScheduleCodes.StateLabel(state.State)} tidak bisa dibatalkan.");

            reason = (reason ?? "").Trim();
            if (reason.Length == 0) throw new ValidationException("Alasan pembatalan wajib diisi.");
            if (reason.Length > 100) throw new ValidationException("Alasan pembatalan maksimal 100 karakter.");

            var before = Snapshot(entity);
            var now = LocalClock.Now;
            entity.status = "0";
            entity.cancel_reason = reason;
            entity.cancelled_by = actingUser;
            entity.cancelled_dt = now;
            entity.updated_by = actingUser;
            entity.updated_dt = now;

            AddHistory(before, "CANCEL", actingUser);
            _audit.LogUpdate(before, Snapshot(entity), Table, actingUser);
            await _context.SaveChangesAsync();

            return ToDto(entity, await LoadNamesAsync(), Now);
        }

        public async Task DeleteAsync(int id, string actingUser)
        {
            var entity = await FindAsync(id);
            var state = ComputeState(entity, Now);
            if (!Permissions(entity, state.State).CanDelete)
                throw new ValidationException("Hanya aturan yang belum pernah berlaku (Akan datang) yang bisa dihapus. Gunakan Batalkan.");

            AddHistory(Snapshot(entity), "DELETE", actingUser);
            _context.RoutesSchedule.Remove(entity);
            _audit.LogDelete(entity, Table, actingUser);
            await _context.SaveChangesAsync();
        }

        private async Task<SwRoutesSchedule> FindAsync(int id) =>
            await _context.RoutesSchedule.FirstOrDefaultAsync(r => r.id == id)
                ?? throw new NotFoundException($"Aturan jadwal '{id}' tidak ditemukan.");

        // ---------------------------------------------------------------- validasi

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>Validasi + normalisasi request ke entity (FASE-3 §5.1). requireFutureEnd: aturan
        /// Sekali yang jam selesainya sudah lewat ditolak (riwayat tidak diisi lewat form).</summary>
        private async Task ApplyAsync(SwRoutesSchedule e, SaveRouteScheduleRequest r, bool requireFutureEnd)
        {
            string name = (r.RuleName ?? "").Trim();
            if (name.Length == 0) throw new ValidationException("Nama aturan wajib diisi.");
            if (name.Length > 50) throw new ValidationException("Nama aturan maksimal 50 karakter.");

            string ruleType = (r.RuleType ?? "").Trim().ToUpperInvariant();
            if (!RouteScheduleCodes.RuleTypes.Contains(ruleType))
                throw new ValidationException("Jenis aturan harus Tutup, Jam Operasional, atau Prioritas Jadwal.");

            string recurrence = (r.Recurrence ?? "").Trim().ToUpperInvariant();
            if (!RouteScheduleCodes.Recurrences.Contains(recurrence))
                throw new ValidationException("Pola harus Sekali, Harian, Mingguan, atau Bulanan.");

            string? routingType = string.IsNullOrWhiteSpace(r.RoutingType) ? null : r.RoutingType.Trim().ToUpperInvariant();
            if (routingType != null && !RouteScheduleCodes.RoutingTypes.Contains(routingType))
                throw new ValidationException("Kategori harus Topup, Bill Payment, atau kosong (semua).");

            string? instId = string.IsNullOrWhiteSpace(r.InstId) ? null : r.InstId.Trim();
            int? nodeId = r.NodeId;

            // cakupan: biller wajib untuk semua jenis (R8, dijaga juga oleh constraint M306); produk kosong =
            // semua produk biller, termasuk Prioritas Jadwal (R9).
            if (nodeId == null)
                throw new ValidationException("Biller wajib diisi.");

            string? nodeName = null;
            if (nodeId.HasValue)
            {
                nodeName = await _context.Nodes.AsNoTracking().Where(n => n.node_id == nodeId).Select(n => n.node_name).FirstOrDefaultAsync()
                    ?? throw new ValidationException($"Biller (node {nodeId}) tidak ditemukan.");
            }

            if (instId != null)
            {
                if (instId.Length > 11) throw new ValidationException("Kode produk maksimal 11 karakter.");
                if (!await _context.Products.AsNoTracking().AnyAsync(p => p.product_code == instId))
                    throw new ValidationException($"Produk '{instId}' tidak ditemukan.");
            }

            if (nodeId.HasValue && instId != null && !await IsBillerOfProductAsync(nodeId.Value, nodeName!, instId, routingType))
                throw new ValidationException($"Biller {nodeName} tidak terdaftar di produk {instId}" +
                    (routingType == null ? "." : $" ({RouteScheduleCodes.RoutingTypeLabel(routingType)})."));

            short? priority = null;
            if (ruleType == RouteScheduleCodes.Priority)
            {
                if (!r.Priority.HasValue || r.Priority < 1 || r.Priority > 99)
                    throw new ValidationException("Priority jadwal wajib 1-99.");
                priority = r.Priority;
            }

            // waktu
            DateTime? startDt = null, endDt = null;
            TimeSpan? timeStart = null, timeEnd = null;
            string? daysOfWeek = null, daysOfMonth = null;
            DateOnly? validFrom = null, validUntil = null;

            if (recurrence == RouteScheduleCodes.Once)
            {
                if (!r.StartDt.HasValue || !r.EndDt.HasValue)
                    throw new ValidationException("Mulai dan Selesai (tanggal + jam) wajib diisi.");
                startDt = TruncateToMinute(r.StartDt.Value);
                endDt = TruncateToMinute(r.EndDt.Value);
                if (endDt <= startDt) throw new ValidationException("Selesai harus setelah Mulai.");
                if (requireFutureEnd && endDt <= Now) throw new ValidationException("Jam selesai sudah lewat.");
            }
            else
            {
                if (!r.TimeStart.HasValue || !r.TimeEnd.HasValue)
                    throw new ValidationException("Jam mulai dan jam selesai wajib diisi.");
                timeStart = TruncateTime(r.TimeStart.Value);
                timeEnd = TruncateTime(r.TimeEnd.Value);

                if (recurrence == RouteScheduleCodes.Weekly)
                    daysOfWeek = NormalizeDays(r.DaysOfWeek, 1, 7, "Hari") ?? throw new ValidationException("Pilih minimal satu hari.");
                if (recurrence == RouteScheduleCodes.Monthly)
                    daysOfMonth = NormalizeDays(r.DaysOfMonth, 1, 31, "Tanggal") ?? throw new ValidationException("Isi minimal satu tanggal (1-31).");

                validFrom = r.ValidFrom;
                validUntil = r.ValidUntil;
                if (validFrom.HasValue && validUntil.HasValue && validUntil < validFrom)
                    throw new ValidationException("Masa berlaku: tanggal akhir sebelum tanggal awal.");
                if (requireFutureEnd && validUntil.HasValue && validUntil < DateOnly.FromDateTime(Now))
                    throw new ValidationException("Masa berlaku sudah lewat.");
            }

            string? notes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim();
            if (notes?.Length > 100) throw new ValidationException("Catatan maksimal 100 karakter.");

            e.rule_name = name;
            e.rule_type = ruleType;
            e.routing_type = routingType;
            e.inst_id = instId;
            e.node_id = nodeId;
            e.priority = priority;
            e.recurrence = recurrence;
            e.start_dt = startDt;
            e.end_dt = endDt;
            e.time_start = timeStart;
            e.time_end = timeEnd;
            e.days_of_week = daysOfWeek;
            e.days_of_month = daysOfMonth;
            e.valid_from = validFrom;
            e.valid_until = validUntil;
            e.notes = notes;
        }

        // biller terdaftar di produk: bill payment = primary/alternate aktif, topup = supplier price aktif
        private async Task<bool> IsBillerOfProductAsync(int nodeId, string nodeName, string instId, string? routingType)
        {
            if (routingType != RouteScheduleCodes.Margin)
            {
                if (await _context.RoutesByInst.AsNoTracking().AnyAsync(x => x.inst_id == instId && x.node_id == nodeId)
                    || await _context.RoutesByInstAlt.AsNoTracking().AnyAsync(x => x.inst_id == instId && x.node_id == nodeId && x.status == "1"))
                    return true;
            }

            if (routingType != RouteScheduleCodes.Product)
            {
                if (await _context.MarginSuppliers.AsNoTracking().AnyAsync(x => x.biller_code == instId && x.supplier_id == nodeName && x.status == "1"))
                    return true;
            }

            return false;
        }

        // "5, 1,3,3" -> "1,3,5"
        private static string? NormalizeDays(string? csv, int min, int max, string label)
        {
            var parts = (csv ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;

            var days = new SortedSet<int>();
            foreach (var p in parts)
            {
                if (!int.TryParse(p, NumberStyles.None, Inv, out var d) || d < min || d > max)
                    throw new ValidationException($"{label} '{p}' tidak valid ({min}-{max}).");
                days.Add(d);
            }
            return string.Join(",", days);
        }

        private static DateTime TruncateToMinute(DateTime d) =>
            DateTime.SpecifyKind(new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0), DateTimeKind.Unspecified);

        private static TimeSpan TruncateTime(TimeSpan t)
        {
            if (t < TimeSpan.Zero || t >= TimeSpan.FromDays(1))
                throw new ValidationException("Jam harus 00:00-23:59.");
            return new TimeSpan(t.Hours, t.Minutes, 0);
        }

        private static bool SameExceptEnd(SwRoutesSchedule a, SwRoutesSchedule b) =>
            a.rule_name == b.rule_name && a.rule_type == b.rule_type && a.routing_type == b.routing_type
            && a.inst_id == b.inst_id && a.node_id == b.node_id && a.priority == b.priority
            && a.recurrence == b.recurrence && a.start_dt == b.start_dt && a.notes == b.notes;

        private static void CopyRule(SwRoutesSchedule from, SwRoutesSchedule to)
        {
            to.rule_name = from.rule_name;
            to.rule_type = from.rule_type;
            to.routing_type = from.routing_type;
            to.inst_id = from.inst_id;
            to.node_id = from.node_id;
            to.priority = from.priority;
            to.recurrence = from.recurrence;
            to.start_dt = from.start_dt;
            to.end_dt = from.end_dt;
            to.time_start = from.time_start;
            to.time_end = from.time_end;
            to.days_of_week = from.days_of_week;
            to.days_of_month = from.days_of_month;
            to.valid_from = from.valid_from;
            to.valid_until = from.valid_until;
            to.notes = from.notes;
        }

        // ---------------------------------------------------------------- status & riwayat

        /// <summary>Status tampilan (FASE-3 §3.6) dan jendela yang sedang berlaku.</summary>
        public static (string State, bool ActiveNow, DateTime? ActiveUntil) ComputeState(SwRoutesSchedule e, DateTime now)
        {
            if (e.status != "1") return (RouteScheduleCodes.StateCancelled, false, null);

            var rule = ToRule(e);
            var window = ScheduleWindow.ActiveWindow(rule, now);

            if (rule.IsOnce)
            {
                if (now < e.start_dt) return (RouteScheduleCodes.StateUpcoming, false, null);
                if (now < e.end_dt) return (RouteScheduleCodes.StateRunning, true, e.end_dt);
                return (RouteScheduleCodes.StateFinished, false, null);
            }

            var today = DateOnly.FromDateTime(now);
            if (window.HasValue) return (RouteScheduleCodes.StateRunning, true, window.Value.End);
            if (e.valid_until.HasValue && e.valid_until < today) return (RouteScheduleCodes.StateFinished, false, null);
            if (e.valid_from.HasValue && e.valid_from > today) return (RouteScheduleCodes.StateUpcoming, false, null);
            return (RouteScheduleCodes.StateActive, false, null);
        }

        public static (bool CanEdit, bool CanEditEndOnly, bool CanCancel, bool CanDelete) Permissions(SwRoutesSchedule e, string state)
        {
            bool once = e.recurrence == RouteScheduleCodes.Once;
            return state switch
            {
                RouteScheduleCodes.StateUpcoming => (true, false, true, true),
                RouteScheduleCodes.StateRunning => (!once, once, true, false),
                RouteScheduleCodes.StateActive => (true, false, true, false),
                _ => (false, false, false, false)
            };
        }

        private static int StateOrder(string state) => state switch
        {
            RouteScheduleCodes.StateRunning => 0,
            RouteScheduleCodes.StateUpcoming => 1,
            RouteScheduleCodes.StateActive => 2,
            RouteScheduleCodes.StateFinished => 3,
            _ => 4
        };

        private static SwRoutesSchedule Snapshot(SwRoutesSchedule e)
        {
            var s = new SwRoutesSchedule { id = e.id, status = e.status };
            CopyRule(e, s);
            s.cancel_reason = e.cancel_reason;
            s.cancelled_by = e.cancelled_by;
            s.cancelled_dt = e.cancelled_dt;
            s.created_by = e.created_by;
            s.created_dt = e.created_dt;
            s.updated_by = e.updated_by;
            s.updated_dt = e.updated_dt;
            return s;
        }

        private void AddHistory(SwRoutesSchedule before, string action, string actingUser)
        {
            _context.RoutesScheduleHist.Add(new SwRoutesScheduleHist
            {
                schedule_id = before.id,
                action = action,
                rule_name = before.rule_name,
                rule_type = before.rule_type,
                routing_type = before.routing_type,
                inst_id = before.inst_id,
                node_id = before.node_id,
                priority = before.priority,
                recurrence = before.recurrence,
                start_dt = before.start_dt,
                end_dt = before.end_dt,
                time_start = before.time_start,
                time_end = before.time_end,
                days_of_week = before.days_of_week,
                days_of_month = before.days_of_month,
                valid_from = before.valid_from,
                valid_until = before.valid_until,
                notes = before.notes,
                status = before.status,
                cancel_reason = before.cancel_reason,
                cancelled_by = before.cancelled_by,
                cancelled_dt = before.cancelled_dt,
                created_by = before.created_by,
                created_dt = before.created_dt,
                updated_by = before.updated_by,
                updated_dt = before.updated_dt,
                changed_by = actingUser,
                changed_dt = LocalClock.Now
            });
        }

        // ---------------------------------------------------------------- cek jadwal & preview

        public async Task<ScheduleCheckResultDto> CheckAsync(DateTime at, string? productId = null, int? nodeId = null, int? denom = null)
        {
            at = TruncateToMinute(at);
            var rules = (await _context.RoutesSchedule.AsNoTracking().Where(r => r.status == "1").ToListAsync()).Select(ToRule);
            var inputs = await LoadProductInputsAsync(productId, nodeId, null, denom);
            var health = await LoadHealthAsync();

            var evaluator = new ScheduleEvaluator(rules);
            var products = inputs.Select(p => evaluator.Evaluate(p, at, health)).ToList();
            return new ScheduleCheckResultDto(at, products, []);
        }

        public async Task<ScheduleCheckResultDto> PreviewAsync(SaveRouteScheduleRequest request, int? editingId = null)
        {
            var draftEntity = new SwRoutesSchedule { id = 0 };
            await ApplyAsync(draftEntity, request, requireFutureEnd: false);
            var draft = ToRule(draftEntity);
            var warnings = new List<string>();
            var now = Now;

            var stored = (await _context.RoutesSchedule.AsNoTracking()
                    .Where(r => r.status == "1" && (editingId == null || r.id != editingId)).ToListAsync())
                .Select(ToRule).ToList();

            var next = ScheduleWindow.NextWindow(draft, now);
            DateTime at;
            if (next == null)
            {
                warnings.Add("Aturan ini tidak akan berlaku lagi (jendela sudah lewat atau tidak ada hari yang cocok).");
                at = now;
            }
            else
            {
                // Jam Operasional berpengaruh di LUAR jendelanya: evaluasi tepat saat jendela berakhir
                at = draft.RuleType == RouteScheduleCodes.Open ? next.Value.End : Max(next.Value.Start, now);

                foreach (var other in stored.Where(o => o.RuleType == draft.RuleType && o.NodeId == draft.NodeId
                             && o.InstId == draft.InstId && (o.RoutingType == null || draft.RoutingType == null || o.RoutingType == draft.RoutingType)))
                {
                    var ow = ScheduleWindow.NextWindow(other, next.Value.Start);
                    if (ow.HasValue && ow.Value.Start < next.Value.End && next.Value.Start < ow.Value.End)
                        warnings.Add($"Tumpang tindih dengan aturan '{other.Name}' ({ow.Value.Start:dd-MM-yyyy HH:mm} – {ow.Value.End:dd-MM-yyyy HH:mm}).");
                }

                // priority jadwal sama untuk biller lain pada produk dan jam yang sama: tidak dibagi (keputusan 2026-09-26)
                if (draft.RuleType == RouteScheduleCodes.Priority)
                {
                    var nodeNames = (await LoadNamesAsync()).Nodes;
                    foreach (var other in stored.Where(o => o.RuleType == RouteScheduleCodes.Priority && o.Priority == draft.Priority
                                 && o.NodeId != draft.NodeId
                                 && (o.InstId == null || draft.InstId == null || o.InstId == draft.InstId)
                                 && (o.RoutingType == null || draft.RoutingType == null || o.RoutingType == draft.RoutingType)))
                    {
                        var ow = ScheduleWindow.NextWindow(other, next.Value.Start);
                        if (ow.HasValue && ow.Value.Start < next.Value.End && next.Value.Start < ow.Value.End)
                        {
                            string otherNode = other.NodeId.HasValue && nodeNames.TryGetValue(other.NodeId.Value, out var nn) ? nn : "biller lain";
                            warnings.Add($"Priority {draft.Priority} sama dengan aturan '{other.Name}' ({otherNode}) pada jam yang sama: transaksi TIDAK dibagi. " +
                                "Hanya satu biller yang dipakai, urutannya mengikuti Routing Mode produk (lihat hasil di bawah); biller lain menjadi cadangan. " +
                                "Beri nilai berbeda bila ingin urutan cadangan yang jelas.");
                        }
                    }
                }
            }

            var evaluator = new ScheduleEvaluator(stored.Append(draft));
            var inputs = await LoadProductInputsAsync(draft.InstId, draft.NodeId, draft.RoutingType, null);
            var health = await LoadHealthAsync();
            var products = inputs.Select(p => evaluator.Evaluate(p, at, health)).ToList();

            foreach (var p in products.Where(p => p.Result == ScheduleCheckResults.Reject))
                warnings.Add($"{p.ProductId} {p.ProductName}: tidak ada biller buka pada {at:dd-MM-yyyy HH:mm} → transaksi ditolak X15.");

            if (draft.RuleType == RouteScheduleCodes.Priority && draft.NodeId.HasValue)
            {
                foreach (var p in products)
                {
                    var b = p.Billers.FirstOrDefault(x => x.NodeId == draft.NodeId);
                    if (b is { Open: false })
                        warnings.Add($"{p.ProductId}: biller tutup saat Prioritas Jadwal berlaku ({b.ClosedBy}); aturan tidak berpengaruh.");
                    if (p.RoutingMode == "STATIC")
                        warnings.Add($"{p.ProductId}: Routing Mode Static, Prioritas Jadwal diabaikan.");
                }
            }

            if (products.Count == 0)
                warnings.Add("Tidak ada produk routing yang terpengaruh aturan ini.");

            return new ScheduleCheckResultDto(at, products, warnings);
        }

        private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

        private async Task<IReadOnlyDictionary<string, string>> LoadHealthAsync() =>
            await _context.RoutesSupplierStatus.AsNoTracking()
                .ToDictionaryAsync(s => s.supplier_id, s => s.status);

        /// <summary>Kandidat per produk untuk evaluasi. Bill payment: primary + alternate aktif, mode dari baris
        /// Product Fees default. Topup: supplier price aktif satu denom (parameter, atau denom terkecil).</summary>
        private async Task<List<ScheduleProductInput>> LoadProductInputsAsync(string? productId, int? nodeId, string? routingType, int? denom)
        {
            var nodes = await _context.Nodes.AsNoTracking().Select(n => new { n.node_id, n.node_name }).ToListAsync();
            var nodeById = nodes.ToDictionary(n => n.node_id, n => n.node_name);
            var nodeByName = nodes.GroupBy(n => n.node_name).ToDictionary(g => g.Key, g => g.First().node_id);
            var productNames = await _context.Products.AsNoTracking()
                .ToDictionaryAsync(p => p.product_code, p => p.product_name);
            var emergency = await _context.RoutesFailoverConfig.AsNoTracking()
                .Where(c => c.inst_id == null).ToDictionaryAsync(c => c.routing_type, c => c.is_active != "1");

            var result = new List<ScheduleProductInput>();

            if (routingType != RouteScheduleCodes.Margin)
            {
                var primaries = await _context.RoutesByInst.AsNoTracking()
                    .Where(x => productId == null || x.inst_id == productId).ToListAsync();
                var alts = await _context.RoutesByInstAlt.AsNoTracking()
                    .Where(x => x.status == "1" && (productId == null || x.inst_id == productId)).ToListAsync();
                var fees = await _context.Fees.AsNoTracking()
                    .Where(f => (f.merchant_id == null || f.merchant_id == "") && (f.submerchant_id == null || f.submerchant_id == "")
                                && (productId == null || f.product_id == productId))
                    .Select(f => new { f.product_id, f.routing_mode, f.static_node_id })
                    .ToListAsync();

                foreach (var pr in primaries.OrderBy(x => x.inst_id))
                {
                    var cands = new List<ScheduleCandidate>();
                    if (nodeById.TryGetValue(pr.node_id, out var pn))
                        cands.Add(new(pr.node_id, pn, 1, pr.fee_sharing, null, pr.lb_weight ?? 0));
                    foreach (var a in alts.Where(a => a.inst_id == pr.inst_id).OrderBy(a => a.priority))
                        if (nodeById.TryGetValue(a.node_id, out var an) && cands.All(c => c.NodeId != a.node_id))
                            cands.Add(new(a.node_id, an, a.priority, a.fee_sharing, null, a.lb_weight ?? 0));

                    if (nodeId.HasValue && cands.All(c => c.NodeId != nodeId)) continue;

                    var fee = fees.FirstOrDefault(f => f.product_id == pr.inst_id);
                    result.Add(new ScheduleProductInput(pr.inst_id, productNames.GetValueOrDefault(pr.inst_id),
                        RouteScheduleCodes.Product, fee?.routing_mode ?? "STATIC", fee?.static_node_id, null,
                        emergency.GetValueOrDefault(RouteScheduleCodes.Product), cands));
                }
            }

            if (routingType != RouteScheduleCodes.Product)
            {
                var margins = await _context.RoutesMargin.AsNoTracking()
                    .Where(x => productId == null || x.inst_id == productId).ToListAsync();
                var codes = margins.Select(m => m.inst_id).ToList();
                var prices = await _context.MarginSuppliers.AsNoTracking()
                    .Where(s => s.status == "1" && s.biller_code != null && codes.Contains(s.biller_code))
                    .ToListAsync();

                foreach (var m in margins.OrderBy(x => x.inst_id))
                {
                    var rows = prices.Where(s => s.biller_code == m.inst_id).ToList();
                    int? d = denom.HasValue && rows.Any(s => s.denom == denom) ? denom : rows.Min(s => s.denom);

                    var cands = rows.Where(s => s.denom == d && s.supplier_id != null && nodeByName.ContainsKey(s.supplier_id))
                        .Select(s => new ScheduleCandidate(nodeByName[s.supplier_id!], s.supplier_id!, s.priority ?? 999,
                            s.margin, s.harga_beli, s.lb_weight ?? 0))
                        .GroupBy(c => c.NodeId).Select(g => g.First())
                        .ToList();

                    if (nodeId.HasValue && cands.All(c => c.NodeId != nodeId)) continue;

                    result.Add(new ScheduleProductInput(m.inst_id, productNames.GetValueOrDefault(m.inst_id),
                        RouteScheduleCodes.Margin, m.routing_mode, m.static_node_id, d,
                        emergency.GetValueOrDefault(RouteScheduleCodes.Margin), cands));
                }
            }

            return result;
        }

        // ---------------------------------------------------------------- mapping

        public static ScheduleRule ToRule(SwRoutesSchedule e) => new()
        {
            Id = e.id,
            Name = string.IsNullOrEmpty(e.rule_name) ? "(aturan ini)" : e.rule_name,
            RuleType = e.rule_type,
            RoutingType = e.routing_type,
            InstId = e.inst_id,
            NodeId = e.node_id,
            Priority = e.priority,
            Recurrence = e.recurrence,
            StartDt = e.start_dt,
            EndDt = e.end_dt,
            TimeStart = e.time_start,
            TimeEnd = e.time_end,
            Days = RouteScheduleCodes.ParseDays(e.recurrence == RouteScheduleCodes.Weekly ? e.days_of_week : e.days_of_month),
            ValidFrom = e.valid_from,
            ValidUntil = e.valid_until,
            IsActive = e.status == "1"
        };

        private sealed record Names(IReadOnlyDictionary<int, string> Nodes, IReadOnlyDictionary<string, string?> Products);

        private async Task<Names> LoadNamesAsync()
        {
            var nodes = await _context.Nodes.AsNoTracking().ToDictionaryAsync(n => n.node_id, n => n.node_name);
            var products = await _context.Products.AsNoTracking().ToDictionaryAsync(p => p.product_code, p => p.product_name);
            return new Names(nodes, products);
        }

        private static RouteScheduleDto ToDto(SwRoutesSchedule e, Names names, DateTime now)
        {
            var (state, activeNow, activeUntil) = ComputeState(e, now);
            var (canEdit, endOnly, canCancel, canDelete) = Permissions(e, state);

            return new RouteScheduleDto(
                e.id, e.rule_name, e.rule_type, e.routing_type, e.inst_id,
                e.inst_id != null && names.Products.TryGetValue(e.inst_id, out var pn) ? pn : null,
                e.node_id, e.node_id.HasValue && names.Nodes.TryGetValue(e.node_id.Value, out var nn) ? nn : null,
                e.priority, e.recurrence, e.start_dt, e.end_dt, e.time_start, e.time_end,
                e.days_of_week, e.days_of_month, e.valid_from, e.valid_until, e.notes, e.status == "1",
                e.cancel_reason, e.cancelled_by, e.cancelled_dt, e.created_by, e.created_dt, e.updated_by, e.updated_dt,
                state,
                ScheduleWindow.Describe(e.recurrence, e.start_dt, e.end_dt, e.time_start, e.time_end,
                    e.days_of_week, e.days_of_month, e.valid_from, e.valid_until),
                activeNow, activeUntil, canEdit, endOnly, canCancel, canDelete);
        }
    }
}
