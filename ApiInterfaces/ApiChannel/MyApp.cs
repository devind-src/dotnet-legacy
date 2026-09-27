using ApiChannel.Common;
using ApiChannel.Constants;
using ApiChannel.Library;
using ApiChannel.Models;
using ApiChannel.Models.Channel;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SyncNet;
using SyncNet.Constants;
using SyncNet.Fees;
using SyncNet.Helpers;
using SyncNet.Library;
using SyncNet.Message;
using SyncNet.Networking;
using SyncNet.Routing;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ApiChannel
{
    internal class MyApp : IAppProcessor
    {
        public const string APPNAME = "API Channel";
        public const string VERSION = "v1.0";

        private readonly AppProcessor _app;
        private readonly XHttpClient _http;

        private readonly BillPaymentFeeCalculator _feeCalculator;
        private readonly PriceRepository _priceRepository;
        private readonly MarginCalculator _marginCalculator;
        private readonly RoutingResolver _routingResolver;
        private readonly Product _product;

        private readonly INbCache _cache;

        public MyApp(INbCache cache)
        {
            _cache = cache;

            _app = new AppProcessor(APPNAME, VERSION, this);

            _feeCalculator = new BillPaymentFeeCalculator();
            _priceRepository = new PriceRepository();
            _marginCalculator = new MarginCalculator(_priceRepository);

            var supplierStatus = new SyncNet.Routing.Failover.SupplierStatusRepository();
            var schedule = new SyncNet.Routing.Schedule.RoutingScheduleRepository();
            _routingResolver = new RoutingResolver(
                new StaticRoutingStrategy(),
                new MarginRoutingStrategy(_priceRepository, supplierStatus, schedule),
                new ProductRoutingStrategy(supplierStatus, schedule),
                supplierStatus,
                schedule);

            _product = new Product();

            _http = new XHttpClient();
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            try
            {
                //read config
                ApiConfig.Initialize();

                //init
                await _feeCalculator.Initialize();
                await _priceRepository.Initialize();
                await _routingResolver.Initialize();
                await _product.Initialize();

                //start app harus paling akhir
                await _app.AppStart(cancellationToken);
            }
            catch (TaskCanceledException)
            {
                await AppProcessor.Logger($"{APPNAME} shutdown");
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger(ex.Message);
            }
        }

        public async Task Stop()
        {
            //stop app
            await _app.AppStop();
        }

        public async Task Resync()
        {
            //read config
            ApiConfig.Initialize();

            //fees
            await _feeCalculator.Initialize();
            await _priceRepository.Initialize();
            await _routingResolver.Initialize();
            await _product.Initialize();
        }

        public async Task OnError(string NodeName, string ConnectionName, Request MsgOriginal, string ErrorMessage)
        {
            string json = JsonConvert.SerializeObject(MsgOriginal);

            await AppProcessor.Logger(ErrorMessage, json, NodeName);
        }

        public Task ProcessMsgFromSinkNode(string NodeName, Request MsgRequest)
        {
            throw new NotImplementedException();
        }

        public async Task ProcessMsgFromSourceNode(string NodeName, string ConnectionName, Response MsgResponse)
        {
            try
            {
                bool found = TryGetFromBuffer(MsgResponse, out CacheModel org);

                //catat health check biller lebih dulu: respons (mis. timeout 1068 dari core) bisa
                //datang saat request HTTP channel sudah ditutup, dan membaca ctx lalu gagal
                if (MsgResponse.tran_type == TranType.INQUIRY || MsgResponse.tran_type == TranType.PAYMENT)
                    await RecordRoutingResult(MsgResponse, found == true ? LatencyMs(org) : null);

                if (found == false || org == null)
                {
                    await AppProcessor.Logger("Original request not found");
                    return;
                }

                //get original request from remote
                string rawurl = org.ctx.Request.Path;
                string MsgRequest = org.json;
                string json = string.Empty;

                //original request
                var req = JsonConvert.DeserializeObject<PaymentModel>(MsgRequest);

                switch (MsgResponse.tran_type)
                {
                    case TranType.INQUIRY:
                        json = Message.ToRemote.Inquiry(MsgRequest, MsgResponse);
                        break;
                    case TranType.PAYMENT:
                        json = Message.ToRemote.Payment(MsgRequest, MsgResponse);
                        break;
                    case TranType.ADVICE:
                        json = Message.ToRemote.Advice(MsgRequest, MsgResponse);
                        break;
                    case TranType.REVERSAL:
                        json = Message.ToRemote.Reversal(MsgRequest, MsgResponse);
                        break;
                }

                //log outgoing
                if (AppProcessor.IsTraceOn() == true)
                {
                    await _app.WriteTrace(NodeName, "RSP", json,
                        AppProcessor.EnumFromTo.To,
                        NetHelper.GetRemoteEP(org.ctx) + org.ctx.Request.Path);
                }

                //send to remote
                if (string.IsNullOrEmpty(json) == false)
                    await _app.ReplyToHttpServer(NodeName, ConnectionName, json, org.ctx);
            }
            catch (Exception ex)
            {
                AppProcessor.WriteLog(ex.Message, "", NodeName);
            }
        }

        public Task ProcessMsgFromRemoteTcp(string NodeName, string ConnectionName, byte[] Bytes, int TotalBytes, EndPoint RemoteEP)
        {
            throw new NotImplementedException();
        }

        public Task ProcessMsgFromRemoteWsClient(string NodeName, string ConnectionName, Request MsgOriginal, string WSMessage, HttpStatusCode StatusCode)
        {
            throw new NotImplementedException();
        }

        public async Task ProcessMsgFromRemoteWsServer(string NodeName, string ConnectionName, string WSMessage, HttpContext Ctx)
        {
            try
            {
                //log incoming
                if (AppProcessor.IsTraceOn() == true)
                {
                    await _app.WriteTrace(NodeName, "REQ", WSMessage,
                        AppProcessor.EnumFromTo.From,
                        NetHelper.GetRemoteEP(Ctx) + Ctx.Request.Path);
                }

                //validate creden
                if (AuthManager.IsCredenValid(Ctx, WSMessage) == false)
                {
                    await ReplyToRemote(NodeName, ConnectionName,
                       RespCode.GetString(RespCode.AUTH_FAILED), Ctx);

                    return;
                }

                #region Transaction
                //message internal
                Request MsgInternal = new();

                string rawurl = Ctx.Request.Path.ToString().ToLower();
                string json;

                switch (rawurl)
                {
                    case RawUrl.ECHO:
                        //echo test
                        json = Message.ToRemote.Echo(WSMessage);

                        //reply
                        await ReplyToRemote(NodeName, ConnectionName, json, Ctx);
                        return;

                    case RawUrl.INQUIRY:
                        MsgInternal = Message.ToInternal.Inquiry(WSMessage);
                        break;
                    case RawUrl.PAYMENT:
                        MsgInternal = Message.ToInternal.Payment(WSMessage);
                        break;
                    case RawUrl.ADVICE:
                        MsgInternal = Message.ToInternal.Advice(WSMessage);
                        break;
                    case RawUrl.REVERSAL:
                        MsgInternal = Message.ToInternal.Reversal(WSMessage);
                        break;

                    default:
                        await ReplyToRemote(NodeName, ConnectionName,
                            RespCode.GetString(RespCode.INVALID_URL), Ctx);
                        return;
                }
                #endregion

                //apply fee & routing. Jadwal Routing: tidak ada biller yang buka = tolak di sini
                //(tidak dikirim ke core); dicatat SDK di failover log (SCHEDULE_CLOSED)
                if (await ApplyFeesAndRouting(MsgInternal) == false)
                {
                    await ReplyToRemote(NodeName, ConnectionName,
                        RespCode.GetString(RespCode.BILLER_CUTOFF), Ctx);

                    return;
                }

                //save to buffer
                if (TryAddToBuffer(MsgInternal, WSMessage, Ctx) == false)
                {
                    await ReplyToRemote(NodeName, ConnectionName,
                        RespCode.GetString(RespCode.DUPLICATE_TRANSACTION), Ctx);

                    return;
                }

                //send to middleware
                await _app.SendToSource(NodeName, ConnectionName, Ctx, MsgInternal);
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger(ex.Message, WSMessage, NodeName);
            }
        }

        public Task TimerAutoSignon(string NodeName)
        {
            throw new NotImplementedException();
        }

        public Task TimerEcho(string NodeName)
        {
            throw new NotImplementedException();
        }

        public Task TimerKeyExchange(string NodeName)
        {
            throw new NotImplementedException();
        }

        public Task NetworkManagement(AppProcessor.EnumNtwrkMgmt cmd, string NodeName, string Param)
        {
            switch (cmd)
            {
                case AppProcessor.EnumNtwrkMgmt.Echo:
                case AppProcessor.EnumNtwrkMgmt.SignOn:
                case AppProcessor.EnumNtwrkMgmt.SignOff:
                case AppProcessor.EnumNtwrkMgmt.KeyChange:
                    break;
            }

            return Task.CompletedTask;
        }


        // false = transaksi ditolak Jadwal Routing (semua biller tutup), jangan dikirim ke core
        private async Task<bool> ApplyFeesAndRouting(Request MsgInternal)
        {
            //topup product
            if (await _product.IsTopup(MsgInternal.receiving_inst_id) == true)
            {
                long denom = (long)MsgInternal.amount_tran;

                //margin routing: supplier dengan margin terbesar, satu siklus satu supplier
                if (_routingResolver.IsMarginRouting(MsgInternal.receiving_inst_id) == true)
                {
                    var decision = await _routingResolver.ResolveMarginAsync(BuildRoutingContext(MsgInternal, denom));
                    if (decision.Rejected == true) return false;

                    string supplierId = decision.NodeName;

                    //set routing
                    MsgInternal.private_data.sink_node = supplierId;

                    //get margin dynamic (dari supplier siklus ini)
                    MsgInternal.fee_data = _marginCalculator.Calculate(MsgInternal.merchant_id,
                        MsgInternal.receiving_inst_id, denom, supplierId);
                }
                else
                {
                    //get margin static
                    string supplierId = await _routingResolver.ResolveStaticSupplierAsync(
                        MsgInternal.receiving_inst_id, denom);

                    MsgInternal.fee_data = _marginCalculator.Calculate(MsgInternal.merchant_id,
                        MsgInternal.receiving_inst_id, denom, supplierId);
                }
            }
            else //bill payment & purchase
            {
                string productId = MsgInternal.receiving_inst_id;

                //routing mode dari baris Product Fees CA ini (atau baris default produk)
                var (mode, staticNodeId) = _feeCalculator.GetRoutingMode(productId, MsgInternal.merchant_id);

                //pilih biller dulu: pada mode dynamic fee biller & switch bergantung pada biller siklus ini.
                //tanpa sink_node core memakai routing statis (primary).
                string nodeName = null;
                if (_routingResolver.HasProductRoute(productId) == true)
                {
                    var decision = await _routingResolver.ResolveProductAsync(
                        BuildRoutingContext(MsgInternal, (long)MsgInternal.amount_tran), mode, staticNodeId);
                    if (decision.Rejected == true) return false;

                    if (decision.Apply == true && string.IsNullOrEmpty(decision.NodeName) == false)
                    {
                        MsgInternal.private_data.sink_node = decision.NodeName;
                        nodeName = decision.NodeName;
                    }
                    else
                    {
                        nodeName = _routingResolver.GetProductPrimary(productId);
                    }
                }

                //calculate fee (Product > Fees, dicari per CA lalu default produk)
                int? sharingFee = RoutingMode.IsDynamic(mode) == true && string.IsNullOrEmpty(nodeName) == false
                    ? _routingResolver.GetProductSharingFee(productId, nodeName)
                    : null;

                MsgInternal.fee_data = await _feeCalculator.GetFees(MsgInternal.merchant_id, productId,
                    MsgInternal.amount_tran, sharingFee);
            }

            return true;
        }

        private static RoutingContext BuildRoutingContext(Request req, long denom)
        {
            return new RoutingContext
            {
                TranType = req.tran_type,
                ProductId = req.receiving_inst_id,
                Denom = denom,
                MerchantId = req.merchant_id,
                TerminalId = req.terminal_id,
                Refnum = req.refnum,
                DateTimeTran = req.datetime_tran,
                TraceNumber = req.trace_number,
                OriginalData = req.original_data
            };
        }

        // dipanggil setiap response INQUIRY/PAYMENT diterima dari supplier/sink node.
        // supplier hanya diketahui kalau routing memakai sink_node (margin routing, atau
        // bill payment dengan mode dynamic / static ke biller pilihan). kegagalan mencatat
        // status tidak boleh menggagalkan balasan ke channel.
        private async Task RecordRoutingResult(Response MsgResponse, int? latencyMs)
        {
            try
            {
                string supplierId = MsgResponse.private_data?.sink_node;
                if (string.IsNullOrEmpty(supplierId) == true) return;

                string productId = MsgResponse.receiving_inst_id;

                string routingType;
                if (await _product.IsTopup(productId) == true)
                {
                    if (_routingResolver.IsMarginRouting(productId) == false) return;
                    routingType = SyncNet.Routing.Failover.SupplierStatusRepository.ROUTING_MARGIN;
                }
                else
                {
                    if (_routingResolver.HasProductRoute(productId) == false) return;
                    routingType = SyncNet.Routing.Failover.SupplierStatusRepository.ROUTING_PRODUCT;
                }

                await _routingResolver.RecordResultAsync(supplierId, routingType, MsgResponse.tran_type,
                    MsgResponse.resp_code, productId, (long)MsgResponse.amount_tran, MsgResponse.trace_number, latencyMs);
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger($"Record routing result: {ex.Message}");
            }
        }

        // lama respons: dari request dikirim ke core sampai respons diterima kembali
        // (termasuk core dan aplikasi biller). null bila waktu kirim tidak tercatat.
        private static int? LatencyMs(CacheModel org)
        {
            if (org == null || org.sent_ts == 0) return null;

            return (int)Math.Min(int.MaxValue, Stopwatch.GetElapsedTime(org.sent_ts).TotalMilliseconds);
        }

        private async Task ReplyToRemote(string NodeName, string ConnectionName, string Json, HttpContext Ctx)
        {
            //log outgoing
            if (AppProcessor.IsTraceOn() == true)
            {
                await _app.WriteTrace(NodeName, "RSP", Json,
                    AppProcessor.EnumFromTo.To,
                    NetHelper.GetRemoteEP(Ctx) + Ctx.Request.Path);
            }

            //reply remote
            await _app.ReplyToHttpServer(NodeName, ConnectionName, Json, Ctx);
        }

        private bool TryGetFromBuffer(Response rsp, out CacheModel obj)
        {
            //composite key
            string key = rsp.tran_type + rsp.datetime_tran + rsp.merchant_id + rsp.trace_number;

            //init object
            return _cache.GetValue(key, out obj);
        }

        private bool TryAddToBuffer(Request reqOct, string reqRmt, HttpContext Ctx)
        {
            //composite key
            string key = reqOct.tran_type + reqOct.datetime_tran + reqOct.merchant_id + reqOct.trace_number;

            //create object
            var cacheObject = new CacheModel
            {
                json = reqRmt,
                ctx = Ctx,
                sent_ts = Stopwatch.GetTimestamp()
            };

            //get original data from remote
            return _cache.Add(key, cacheObject);
        }
    }
}
