using SyncNet.Constants;
using SyncNet.Models;
using SyncNet.Routing.Failover;
using SyncNet.Routing.Schedule;
using System;
using System.Threading.Tasks;

namespace SyncNet.Routing
{
    // data transaksi yang dibutuhkan untuk memilih biller sebuah siklus
    public class RoutingContext
    {
        public string TranType { get; set; }
        public string ProductId { get; set; }
        public long Denom { get; set; }
        public string MerchantId { get; set; }
        public string TerminalId { get; set; }
        public string Refnum { get; set; }
        public string DateTimeTran { get; set; }
        public string TraceNumber { get; set; }
        public string OriginalData { get; set; } // advice/reversal: tran_type payment + datetime + trace
    }

    public class RoutingDecision
    {
        // rc penolakan Jadwal Routing yang dicatat di failover log; aplikasi channel membalas CA dengan
        // kode channel yang sama (prefix X = ditolak interface channel, keputusan R1/R2 Fase 3)
        public const string SCHEDULE_REJECT_RC = "X15";

        // false = jangan isi sink_node, biarkan core memakai routing statis
        public bool Apply { get; set; }
        public string NodeName { get; set; } = string.Empty;

        // true = tidak ada biller yang buka menurut Jadwal Routing: transaksi jangan dikirim ke core,
        // balas CA dengan rc cut-off. ScheduleId = aturan penyebab.
        public bool Rejected { get; set; }
        public int? ScheduleId { get; set; }

        public static readonly RoutingDecision None = new() { Apply = false };
    }

    // titik masuk tunggal untuk resolusi routing produk.
    // strategy baru (mis. by period, by target volume) tinggal diimplementasikan
    // dan didaftarkan di sini.
    public class RoutingResolver
    {
        private readonly StaticRoutingStrategy _staticRouting;
        private readonly MarginRoutingStrategy _marginRouting;
        private readonly ProductRoutingStrategy _productRouting;
        private readonly SupplierStatusRepository _supplierStatus;
        private readonly RoutingScheduleRepository _schedule;
        private readonly StickyRouteResolver _sticky;

        public RoutingResolver(StaticRoutingStrategy staticRouting, MarginRoutingStrategy marginRouting,
            ProductRoutingStrategy productRouting, SupplierStatusRepository supplierStatus,
            RoutingScheduleRepository schedule)
        {
            _staticRouting = staticRouting;
            _marginRouting = marginRouting;
            _productRouting = productRouting;
            _supplierStatus = supplierStatus;
            _schedule = schedule;
            _sticky = new StickyRouteResolver();
        }

        // dipanggil saat start dan saat perintah RESYNC
        public async Task Initialize()
        {
            await _supplierStatus.Initialize();
            await _schedule.Initialize();
            await _staticRouting.Initialize();
            await _marginRouting.Initialize();
            await _productRouting.Initialize();
        }

        public bool IsMarginRouting(string productId)
        {
            return _marginRouting.IsApplicable(productId);
        }

        // bill payment/purchase yang punya route di Routing > Product (sw_routes_by_inst)
        public bool HasProductRoute(string productId)
        {
            return _productRouting.HasRoute(productId);
        }

        public string GetProductPrimary(string productId)
        {
            return _productRouting.GetPrimary(productId);
        }

        // sharing fee biller untuk produk ini (null = belum diisi)
        public int? GetProductSharingFee(string productId, string nodeName)
        {
            return _productRouting.GetSharingFee(productId, nodeName);
        }

        public Task<string> ResolveStaticSupplierAsync(string productId, long denom)
        {
            return _staticRouting.ResolveSupplierAsync(productId, denom);
        }

        // topup (routing by margin). produk margin selalu dirutekan lewat sink_node:
        // mode STATIC atau tombol darurat OFF = supplier urutan pertama tanpa failover.
        public Task<RoutingDecision> ResolveMarginAsync(RoutingContext ctx)
        {
            return ResolveCycleAsync(SupplierStatusRepository.ROUTING_MARGIN, ctx,
                _marginRouting.IsFailoverActive(ctx.ProductId),
                applyWhenInactive: true,
                () => _marginRouting.SelectSupplierAsync(ctx.ProductId, ctx.Denom));
        }

        // bill payment & purchase (routing by product). mode dan biller static berasal dari
        // baris Product Fees yang berlaku untuk CA ini.
        //   STATIC tanpa biller pilihan : routing statis oleh core (primary), tidak disentuh
        //   STATIC dengan biller pilihan: selalu ke biller itu, tanpa failover
        //   mode dynamic               : dipilih per siklus; tombol darurat OFF = routing statis oleh core
        // Jadwal Routing (tombol darurat ON): biller static / primary yang tutup = ditolak.
        public async Task<RoutingDecision> ResolveProductAsync(RoutingContext ctx, string mode, int? staticNodeId)
        {
            mode = RoutingMode.Normalize(mode);

            if (mode == RoutingMode.STATIC)
            {
                string node = staticNodeId.HasValue == true
                    ? _productRouting.GetNodeName(ctx.ProductId, staticNodeId.Value)
                    : null;

                string target = string.IsNullOrEmpty(node) == true ? _productRouting.GetPrimary(ctx.ProductId) : node;
                var rejected = await CheckStaticScheduleAsync(SupplierStatusRepository.ROUTING_PRODUCT, ctx, target);
                if (rejected != null) return rejected;

                return string.IsNullOrEmpty(node) == true
                    ? RoutingDecision.None
                    : new RoutingDecision { Apply = true, NodeName = node };
            }

            return await ResolveCycleAsync(SupplierStatusRepository.ROUTING_PRODUCT, ctx,
                _productRouting.IsDynamicActive(ctx.ProductId, mode),
                applyWhenInactive: false,
                () => _productRouting.SelectNodeAsync(ctx.ProductId, mode));
        }

        // satu siklus satu biller: dipilih sekali di transaksi pertama siklus, transaksi
        // berikutnya mengikuti tanpa memeriksa status eligible maupun jadwal.
        private async Task<RoutingDecision> ResolveCycleAsync(string routingType, RoutingContext ctx,
            bool failoverActive, bool applyWhenInactive, Func<Task<RouteSelection>> pickNew)
        {
            if (failoverActive == false)
            {
                if (applyWhenInactive == false) return RoutingDecision.None;

                //topup STATIC / tombol darurat OFF: strategi sendiri yang memutuskan jadwal berlaku atau tidak
                var fixedSel = await pickNew();
                if (fixedSel.Rejected == true && await OpensCycleAsync(ctx) == true)
                    return await RejectAsync(routingType, ctx, fixedSel);

                if (fixedSel.Rejected == false)
                    await SaveStaticCycleAsync(routingType, ctx, fixedSel.NodeName);

                return new RoutingDecision { Apply = true, NodeName = fixedSel.NodeName ?? string.Empty };
            }

            string node = null;

            try
            {
                switch (ctx.TranType)
                {
                    case TranType.INQUIRY:
                        var inq = await pickNew();
                        if (inq.Rejected == true) return await RejectAsync(routingType, ctx, inq);

                        node = inq.NodeName;
                        await SaveCycleAsync(routingType, ctx, node, inquiry: true);
                        break;

                    case TranType.PAYMENT:
                        var entry = await _sticky.FindByRefAsync(ctx.MerchantId, ctx.TerminalId, ctx.Refnum);
                        if (entry != null)
                        {
                            node = entry.NodeName;
                            await _sticky.SetSwitchKeyAsync(entry.Id, CurrentSwitchKey(ctx));
                        }
                        else
                        {
                            //topup tanpa inquiry: transaksi ini yang membuka siklus
                            var pay = await pickNew();
                            if (pay.Rejected == true) return await RejectAsync(routingType, ctx, pay);

                            node = pay.NodeName;
                            await SaveCycleAsync(routingType, ctx, node, inquiry: false);
                        }
                        break;

                    case TranType.ADVICE:
                    case TranType.REVERSAL:
                        node = await _sticky.FindNodeForOriginalAsync(ctx.MerchantId, ctx.TerminalId, ctx.Refnum,
                            StickyRouteResolver.BuildOriginalSwitchKey(ctx.OriginalData, ctx.TerminalId));
                        break;
                }
            }
            catch (Exception ex)
            {
                //gagal baca/tulis catatan siklus tidak boleh menggagalkan transaksi
                await AppProcessor.Logger($"Sticky routing: {ex.Message}", ctx.Refnum);
            }

            //fallback (mis. catatan siklus tidak ditemukan): tidak menolak, kandidat pertama dipakai
            if (string.IsNullOrEmpty(node) == true)
                node = (await pickNew()).NodeName;

            return new RoutingDecision { Apply = true, NodeName = node ?? string.Empty };
        }

        // biller Static (bill payment) yang tutup menurut jadwal: tolak transaksi yang membuka siklus.
        // siklus yang sudah dibuka sebelum jadwal berlaku tetap diteruskan (keputusan Q3).
        private async Task<RoutingDecision> CheckStaticScheduleAsync(string routingType, RoutingContext ctx, string target)
        {
            if (string.IsNullOrEmpty(target) == true) return null;
            if (_supplierStatus.IsFailoverEnabled(routingType) == false) return null;

            var closedBy = _schedule.ClosedBy(routingType, ctx.ProductId, target, _schedule.Now);
            if (closedBy != null)
            {
                if (await OpensCycleAsync(ctx) == false) return null;
                return await RejectAsync(routingType, ctx, RouteSelection.Reject(target, closedBy.Id));
            }

            await SaveStaticCycleAsync(routingType, ctx, target);
            return null;
        }

        // Static tidak mencatat siklus. bila biller punya aturan jadwal, inquiry dicatat supaya payment
        // yang datang setelah jadwal Tutup dimulai tetap diteruskan ke biller yang sama.
        private async Task SaveStaticCycleAsync(string routingType, RoutingContext ctx, string node)
        {
            if (ctx.TranType != TranType.INQUIRY || _schedule.HasRulesFor(node) == false) return;

            try
            {
                await SaveCycleAsync(routingType, ctx, node, inquiry: true);
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger($"Sticky routing: {ex.Message}", ctx.Refnum);
            }
        }

        // transaksi ini membuka siklus baru? INQUIRY selalu; PAYMENT bila belum ada catatan siklus
        // (topup / purchase tanpa inquiry); ADVICE/REVERSAL tidak pernah ditolak.
        private async Task<bool> OpensCycleAsync(RoutingContext ctx)
        {
            switch (ctx.TranType)
            {
                case TranType.INQUIRY:
                    return true;
                case TranType.PAYMENT:
                    try
                    {
                        return await _sticky.FindByRefAsync(ctx.MerchantId, ctx.TerminalId, ctx.Refnum) == null;
                    }
                    catch (Exception ex)
                    {
                        await AppProcessor.Logger($"Sticky routing: {ex.Message}", ctx.Refnum);
                        return true;
                    }
                default:
                    return false;
            }
        }

        private async Task<RoutingDecision> RejectAsync(string routingType, RoutingContext ctx, RouteSelection sel)
        {
            try
            {
                await _supplierStatus.LogScheduleRejectAsync(routingType, ctx.ProductId,
                    routingType == SupplierStatusRepository.ROUTING_MARGIN ? ctx.Denom : null,
                    ctx.TraceNumber, sel.NodeName, RoutingDecision.SCHEDULE_REJECT_RC, sel.ScheduleId);
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger($"Schedule reject log: {ex.Message}", ctx.Refnum);
            }

            return new RoutingDecision { Apply = false, Rejected = true, ScheduleId = sel.ScheduleId, NodeName = sel.NodeName };
        }

        private async Task SaveCycleAsync(string routingType, RoutingContext ctx, string node, bool inquiry)
        {
            if (string.IsNullOrEmpty(node) == true) return;

            string key = CurrentSwitchKey(ctx);

            await _sticky.SaveAsync(new TranMapModel
            {
                MerchantId = ctx.MerchantId,
                TerminalId = ctx.TerminalId,
                Refnum = ctx.Refnum,
                RoutingType = routingType,
                InstId = ctx.ProductId,
                Denom = routingType == SupplierStatusRepository.ROUTING_MARGIN ? (int?)ctx.Denom : null,
                NodeName = node,
                InquirySwitchKey = inquiry ? key : null,
                SwitchKey = inquiry ? null : key
            });
        }

        // switch_key transaksi yang sedang diproses (tran_type + datetime + trace + terminal)
        private static string CurrentSwitchKey(RoutingContext ctx)
        {
            return StickyRouteResolver.BuildSwitchKey(ctx.TranType, ctx.DateTimeTran, ctx.TraceNumber, ctx.TerminalId);
        }

        // dipanggil dari titik proses response transaksi (di luar SDK Routing) setelah rc
        // sebuah transaksi ke supplier diketahui. hanya INQUIRY dan PAYMENT yang dicatat;
        // advice/reversal tidak dihitung sebagai kegagalan baru. aturan per tran type (mis.
        // timeout dan pending hanya dari PAYMENT, inquiry sukses hanya mereset rc gagal) ada
        // di SupplierStatusRepository. latencyMs = lama respons yang diukur aplikasi channel.
        public async Task RecordResultAsync(string supplierId, string routingType, string tranType,
            string rcCode, string productId, long? denom, string traceNumber, int? latencyMs = null)
        {
            if (_supplierStatus.IsFailoverEnabled(routingType) == false) return;
            if (tranType != TranType.INQUIRY && tranType != TranType.PAYMENT) return;

            await _supplierStatus.RecordResponseAsync(supplierId, routingType, tranType == TranType.PAYMENT,
                rcCode, latencyMs, productId, denom, traceNumber);
        }
    }
}
