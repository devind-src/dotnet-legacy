using Newtonsoft.Json;
using SyncNet.Common;
using SyncNet.Constants;
using SyncNet.DbRepository;
using SyncNet.Helpers;
using SyncNet.Library;
using SyncNet.Models.Common;
using SyncNet.Models.Networking;
using SyncNet.Networking;
using SyncNet.Nodes;
using SyncNet.Services;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SyncNet
{
    class MyApp
    {
        //constanta
        public const string APPNAME = "Processing Manager";
        public const string VERSION = "1.0";

        #region Init Object
        //command
        private readonly XTcpListener _cmd;

        //logger
        private static NbLogger _logger;

        //services
        private static MainNode _nodes;
        private static HsmService _hsm;
        private static RoutingService _route;
        private static HotCardService _hotcard;
        private static BusinessDateService _bsndate;

        //trace
        private static Channel<string> _channel;
        private static bool _IsTraceOn = false;
        #endregion

        public MyApp(Channel<string> channel)
        {
            _channel = channel;

            //initialize app config
            AppConfig.Initialize();

            //init database 
            DbMgr.Initialize();

            //logger
            _logger = new NbLogger(APPNAME);

            //init after connect to db success
            _hotcard = new HotCardService();
            _route = new RoutingService();
            _nodes = new MainNode();
            _bsndate = new BusinessDateService();
            _hsm = new HsmService();

            //commander
            _cmd = new XTcpListener();
            _cmd.OnDataArrival += RequestCommand;
        }

        public async Task InitAsync()
        {
            //logger
            await Logger($"{APPNAME} started");

            //TODO: lainnya jika ada
        }

        public async Task Start(CancellationToken stoppingToken)
        {
            try
            {
                //validate license
                if (LicenseManager.ValidateLicense() == false)
                {
                    await Logger($"Permission to run application denied");
                    return;
                }

                //open port command
                await InitPortService();

                //load data
                await _nodes.Start();
                await _bsndate.Start();
                await _route.Resync();
                await _hotcard.Resync();

                //update status app
                await DbMgr.UpdateApp(DbMgr.EnumStatusApp.UP);
            }
            catch (Exception ex)
            {
                await Logger(ex.Message);
            }
        }

        public async Task Stop()
        {
            try
            {
                //logger
                await Logger($"{APPNAME} stop");

                //update status app
                await DbMgr.UpdateApp(DbMgr.EnumStatusApp.DOWN);

                //close command
                await _cmd.StopAsync();

                //close nodes
                await _nodes.Stop();

                _bsndate.Dispose();
            }
            catch (Exception ex)
            {
                await Logger(ex.Message);
            }
        }

        #region Messaging
        public static async Task OnDataArrivalSourceAsync(string SourceNode, Message.Request MsgRequest)
        {
            try
            {
                //set routing for saving into database
                var (broute, destNode) = await _route.TryGetRouting(MsgRequest);

                //set dest node
                if (string.IsNullOrEmpty(destNode) == false)
                    MsgRequest.private_data.sink_node = destNode ?? "";

                //check tran type
                if (CheckTranType(MsgRequest) == false)
                {
                    await ReplyAndInsert(SourceNode, MsgRequest, RespCodeOct.RC30_FORMAT_INVALID, "Invalid tran type");

                    await Logger("Invalid tran type", JsonConvert.SerializeObject(MsgRequest));

                    return;
                }

                //validate message
                if (CheckMessage(MsgRequest, out string errmsg) == false)
                {
                    await ReplyAndInsert(SourceNode, MsgRequest, RespCodeOct.RC30_FORMAT_INVALID, errmsg);

                    await Logger(errmsg, JsonConvert.SerializeObject(MsgRequest));

                    return;
                }

                //terminal logon
                if (MsgRequest.tran_type == TranType.TERMLOGON)
                {
                    await RequestKeyTerminal(MsgRequest);
                    return;
                }

                //key exchange
                if (MsgRequest.tran_type == TranType.KEYCHANGE)
                {
                    await RequestKeyChange(MsgRequest);
                    return;
                }

                //check closing time
                if (await CheckClosingTime(SourceNode, MsgRequest) == -1) return;

                //validate advice, refund, reversal
                if (await CheckAdvRev(SourceNode, MsgRequest) == -1) return;

                //validate va transaction
                if (await CheckVATrans(SourceNode, MsgRequest) == true) return;

                //check routing
                if (broute == false)
                {
                    await ReplyAndInsert(SourceNode, MsgRequest, RespCodeOct.RC92_ROUTING_NOT_FOUND, "Routing not found");
                    return;
                }

                //check hotcard
                if (_hotcard.IsHotCard(MsgRequest.pan) == true)
                {
                    await ReplyAndInsert(SourceNode, MsgRequest, RespCodeOct.RC57_TRAN_NOT_PERMITTED, "Transaction not permitted to cardholder");
                    return;
                }

                //save orig date settle for reply decline transaction
                string date_settle = MsgRequest.date_settle;

                //set date settlement
                MsgRequest.date_settle = _bsndate.getDateSettlement(MsgRequest.private_data.calendar_name);

                //insert db & debet/credit va
                int ret = await DbMgr.InsertTrx(MsgRequest);

                //insert db failed
                if (ret != 0)
                {
                    //set back to orig date settle
                    MsgRequest.date_settle = date_settle;

                    switch (ret)
                    {
                        case -50://va not found
                            await ReplyAndInsert(SourceNode, MsgRequest, RespCodeOct.RC50_VA_NOT_FOUND, "Virtual account not found");
                            break;
                        case -51://saldo tdk cukup
                            await ReplyAndInsert(SourceNode, MsgRequest, RespCodeOct.RC51_INSUFICIENT_FUND, "Insuficient fund");
                            break;
                        default:
                            await ReplyWithoutUpdate(SourceNode, MsgRequest, RespCodeOct.RC06_TRAN_FAILED, "Transaction failed");
                            break;
                    }

                    return;
                }

                //auto reply reversal
                if (MsgRequest.tran_type == TranType.REVERSAL &&
                    MsgRequest.private_data.mode_timeout == ModeTimeout.AutoReverse &&
                    MsgRequest.private_data.auto_reply_rev == "1")
                {
                    //set back to orig date settle
                    MsgRequest.date_settle = date_settle;

                    //reversal di reply tanpa meneruskan ke sinknode
                    //sinknode otomatis akan kirim reversal pada saat trx timeout
                    await ReplyAutoReversal(SourceNode, MsgRequest);
                    return;
                }

                //check pin translate
                if (CheckPinTranslate(MsgRequest) == true)
                {
                    string ErrCode;

                    if (MsgRequest.security.hsm_cmd == Message.Security.EnumHsmCommand.TranslatePinblockTerminal)
                    {
                        //translate pinblock terminal
                        ErrCode = await _hsm.TranslatePinblockTerminal(MsgRequest);
                    }
                    else
                    {
                        //default translate pinblock zone
                        ErrCode = await _hsm.TranslatePinblock(MsgRequest);
                    }

                    //validate
                    if (ErrCode != HsmErrCode.SUCCESS)
                    {
                        await ReplyAndUpdate(SourceNode, MsgRequest, RespCodeOct.RC96_SECURITY_FAILED, "PIN translate failed");
                        return;
                    }
                }

                //send to sink
                await _nodes.SendToSink(MsgRequest.private_data.sink_node, MsgRequest);
            }
            catch (Exception ex)
            {
                await Logger(ex.Message);
            }
        }

        public static async Task OnDataArrivalSinkAsync(string SinkNode, Message.Response MsgResponse)
        {
            try
            {
                //get source node
                string SourceNode = MsgResponse.private_data.source_node;

                //update database
                await DbMgr.UpdateTrx(MsgResponse);

                //req adv or rev from core
                if (MsgResponse.private_data.is_req_internal == true) return;

                //reply response to source
                await _nodes.SendToSource(SourceNode, MsgResponse);
            }
            catch (Exception ex)
            {
                await Logger(ex.Message);
            }
        }

        public static async void OnMessageTimeout(string SinkNode, Message.Request MsgRequest)
        {
            try
            {
                //first time timeout reply to acq
                if (MsgRequest.private_data.retry_send == 0)
                {
                    //construct msg reply
                    var rsp = new Message.Response(MsgRequest)
                    {
                        resp_code = RespCodeOct.RC68_TIMEOUT,
                        resp_message = "Timeout",
                        authorized_by = AuthTran.INTERNAL
                    };

                    //get source node
                    string SourceNode = MsgRequest.private_data.source_node;

                    //send
                    await _nodes.SendToSource(SourceNode, rsp);

                    //update trx timeout
                    await DbMgr.UpdateTrx(rsp);
                }

                //mode timeout NONE - do not send auto adv/rev
                if (MsgRequest.private_data.mode_timeout == ModeTimeout.None) return;

                //check retry send
                MsgRequest.private_data.retry_send++;
                if (MsgRequest.private_data.retry_send > MsgRequest.private_data.max_retry_send) return;

                //change msgtype & tran type
                switch (MsgRequest.private_data.mode_timeout)
                {
                    case ModeTimeout.AutoReverse:
                        MsgRequest.msgtype = "0420";
                        MsgRequest.tran_type = TranType.REVERSAL;
                        break;
                    case ModeTimeout.AutoAdvice:
                        MsgRequest.msgtype = "0220";
                        MsgRequest.tran_type = TranType.ADVICE;
                        break;
                    default:
                        return;
                }

                //request from core
                MsgRequest.private_data.is_req_internal = true;

                //save repeat reversal to db
                if (MsgRequest.private_data.save_repeat_reversal == "1")
                {
                    //generate unique value
                    MsgRequest.datetime_tran = DateTime.Now.ToString("yyyyMMddHHmmss");
                    MsgRequest.trace_number = NbSystem.GetRandomNumber(12);

                    //insert advice|reversal
                    await DbMgr.InsertTrxAdvRev(MsgRequest);
                }
                else
                {
                    //save first reversal only
                    if (MsgRequest.private_data.retry_send == 1)
                    {
                        //insert advice|reversal
                        await DbMgr.InsertTrxAdvRev(MsgRequest);
                    }
                }

                //send message
                await _nodes.SendToSink(SinkNode, MsgRequest);
            }
            catch (Exception ex)
            {
                await Logger(ex.Message);
            }
        }

        public static async Task OnCutover(string CalendarName)
        {
            await _nodes.SendCutover(CalendarName);
        }
        #endregion

        #region Logger
        public static bool IsTraceOn()
        {
            return _IsTraceOn;
        }

        public static async Task Logger(string msg)
        {
            //Console.WriteLine(msg);

            await _logger.LogAsync(msg);
        }

        public static async Task Logger(string msg, string detail)
        {
            //Console.WriteLine(msg);

            await _logger.LogAsync(msg, detail);
        }

        public static async Task LogTraceIncoming(string MsgType, string WSMessage, string NodeName, string RemoteAddress)
        {
            if (_IsTraceOn == false) return;

            try
            {
                string header = $"<{MsgType}> Message from {NodeName} {RemoteAddress}"; //e.g. <0200> message from TM

                var req = new LogModel
                {
                    LogType = LogType.Transaction,
                    Datetime = DateTime.Now,
                    AppName = APPNAME,
                    FileName = NodeName,
                    Title = header,
                    Detail = WSMessage
                };

                //construct log
                string json = JsonConvert.SerializeObject(req);

                // Kirim ke channel
                await _channel.Writer.WriteAsync(json);              
            }
            catch (Exception ex)
            {
                await Logger(ex.Message);
            }
        }

        public static async Task LogTraceOutgoing(string MsgType, string WSMessage, string NodeName, string RemoteAddress)
        {
            if (_IsTraceOn == false) return;

            try
            {
                string header = $"<{MsgType}> Message to {NodeName} {RemoteAddress}"; //e.g. <0200> message to TM

                var req = new LogModel
                {
                    LogType = LogType.Transaction,
                    Datetime = DateTime.Now,
                    AppName = APPNAME,
                    FileName = NodeName,
                    Title = header,
                    Detail = WSMessage
                };

                //construct log
                string json = JsonConvert.SerializeObject(req);

                // Kirim ke channel
                await _channel.Writer.WriteAsync(json);
            }
            catch (Exception ex)
            {
                await Logger(ex.Message);
            }
        }
        #endregion

        #region Validate
        private static bool CheckMessage(Message.Request req, out string errmsg)
        {
            bool bval = false;

            //initial value
            errmsg = string.Empty;

            //check tran type
            switch (req.tran_type)
            {
                case TranType.KEYCHANGE:
                    bval = true;

                    //if (string.IsNullOrEmpty(req.merchant_id) == true)
                    //    errmsg = "Merchant ID mandatory";
                    //else
                    //    bval = true;

                    break;

                case TranType.PINCHANGE:
                    if (string.IsNullOrEmpty(req.pan) == true)
                        errmsg = "PAN mandatory";
                    else if (string.IsNullOrEmpty(req.security.pindata) == true)
                        errmsg = "PIN data mandatory";
                    else if (string.IsNullOrEmpty(req.security.miscdata) == true)
                        errmsg = "MISC data mandatory";
                    else
                        bval = true;

                    break;

                case TranType.PAYMENT:
                case TranType.PURCHASE:
                case TranType.TRANSFER:
                    //check empty
                    if (string.IsNullOrEmpty(req.datetime_tran) == true)
                        errmsg = "Date time tran mandatory";
                    else if (string.IsNullOrEmpty(req.tran_type) == true)
                        errmsg = "Tran Type mandatory";
                    else if (string.IsNullOrEmpty(req.trace_number) == true)
                        errmsg = "Trace number mandatory";
                    else if (string.IsNullOrEmpty(req.merchant_id) == true)
                        errmsg = "Merchant ID mandatory";
                    else if (string.IsNullOrEmpty(req.refnum) == true)
                        errmsg = "Refnum mandatory";

                    else if (string.IsNullOrEmpty(req.pan) == true && string.IsNullOrEmpty(req.receiving_inst_id) == true)
                        errmsg = "PAN or Product ID mandatory for routing";
                    else if (string.IsNullOrEmpty(req.receiving_inst_id) == false && string.IsNullOrEmpty(req.to_acc_number) == true)
                        errmsg = "To Account Number mandatory";

                    //validate length
                    else if (req.datetime_tran.Length != 14)
                        errmsg = "Length date time tran must be 14 digit";
                    else if (req.trace_number.Length != 12)
                        errmsg = "Length trace number must be 12 digit";
                    else if (req.refnum.Length > 30)
                        errmsg = "Max length refnum 30 digit";
                    else
                        bval = true;

                    break;

                default:
                    //check empty
                    if (string.IsNullOrEmpty(req.datetime_tran) == true)
                        errmsg = "Date time tran mandatory";
                    else if (string.IsNullOrEmpty(req.tran_type) == true)
                        errmsg = "Tran Type mandatory";
                    else if (string.IsNullOrEmpty(req.trace_number) == true)
                        errmsg = "Trace number mandatory";
                    else if (string.IsNullOrEmpty(req.merchant_id) == true)
                        errmsg = "Merchant ID mandatory";
                    else if (string.IsNullOrEmpty(req.refnum) == true)
                        errmsg = "Refnum mandatory";

                    //validate length
                    else if (req.datetime_tran.Length != 14)
                        errmsg = "Length date time tran must be 14 digit";
                    else if (req.trace_number.Length != 12)
                        errmsg = "Length trace number must be 12 digit";
                    else if (req.refnum.Length > 30)
                        errmsg = "Max length refnum 30 digit";
                    else
                        bval = true;

                    break;
            }

            return bval;
        }

        private static bool CheckTranType(Message.Request req)
        {
            bool bval = false;

            switch (req.tran_type)
            {
                case TranType.MINISTATEMENT:
                case TranType.INQBALANCE:
                case TranType.WITHDRAWAL:
                case TranType.DEPOSIT:

                case TranType.INQUIRY:
                case TranType.PAYMENT:
                case TranType.PURCHASE:
                case TranType.TRANSFER:

                case TranType.DEBET:
                case TranType.CREDIT:
                case TranType.ADJUSTMENT:

                case TranType.VOID:
                case TranType.ADVICE:
                case TranType.REFUND:
                case TranType.REVERSAL:

                case TranType.VTOPUP:
                case TranType.VADJUST:
                case TranType.VBALANCE:

                case TranType.ADMIN:
                case TranType.SETTLEMENT:
                case TranType.PINCHANGE:
                case TranType.KEYCHANGE:
                    bval = true;
                    break;
            }

            return bval;
        }

        private static bool CheckPinTranslate(Message.Request req)
        {
            bool bval = false;

            //check pin translate
            if (req.security.pin_translate == "1")
            {
                if (string.IsNullOrEmpty(req.security.pindata) == false)
                    bval = true;
            }

            return bval;
        }

        private static async Task<bool> CheckVATrans(string SourceNode, Message.Request MsgRequest)
        {
            switch (MsgRequest.tran_type)
            {
                //internal transaction
                case TranType.VTOPUP:
                case TranType.VADJUST:
                case TranType.VBALANCE:
                    Message.Response rsp = await DbMgr.VirtualAccount(MsgRequest);
                    rsp.authorized_by = AuthTran.INTERNAL;

                    //send
                    await _nodes.SendToSource(SourceNode, rsp);

                    return true;
            }

            return false;
        }

        private static async Task<int> CheckAdvRev(string SourceNode, Message.Request MsgRequest)
        {
            switch (MsgRequest.tran_type)
            {
                case TranType.VOID:
                case TranType.REFUND:
                    //check original data
                    if (string.IsNullOrEmpty(MsgRequest.original_data) == false &&
                        await DbMgr.IsOriginalExist(MsgRequest) == false)
                    {
                        await ReplyAndInsert(SourceNode, MsgRequest, RespCodeOct.RC63_PAYMENT_NOT_FOUND, "Payment not found");
                        return -1;
                    }

                    break;
                case TranType.ADVICE:
                case TranType.REVERSAL:
                    if (await DbMgr.IsOriginalExist(MsgRequest) == false)
                    {
                        await ReplyAndInsert(SourceNode, MsgRequest, RespCodeOct.RC63_PAYMENT_NOT_FOUND, "Payment not found");
                        return -1;
                    }

                    break;
            }

            return 0;
        }

        private static async Task<int> CheckClosingTime(string SourceNode, Message.Request MsgRequest)
        {
            int enable_closing = MsgRequest.private_data.enable_closing;
            int time_start = NbConvert.ToInt(MsgRequest.private_data.closing_time_start.Replace(":", ""));
            int time_end = NbConvert.ToInt(MsgRequest.private_data.closing_time_end.Replace(":", ""));

            if (enable_closing == 1)
            {
                int now = Convert.ToInt32(DateTime.Now.ToString("HHmmss"));

                if (time_start <= time_end)
                {
                    if (now >= time_start && now <= time_end)
                    {
                        await ReplyAndInsert(SourceNode, MsgRequest, RespCodeOct.RC90_CUTOFF, "Cut-off in progress");
                        return -1;
                    }
                }
                else
                {
                    if (now >= time_start || now <= time_end)
                    {
                        await ReplyAndInsert(SourceNode, MsgRequest, RespCodeOct.RC90_CUTOFF, "Cut-off in progress");
                        return -1;
                    }
                }
            }

            return 0;
        }
        #endregion

        #region HSM
        private static async Task RequestKeyChange(Message.Request MsgRequest)
        {
            try
            {
                //get misc data
                string ErrCode;

                if (string.IsNullOrEmpty(MsgRequest.security.miscdata) == true)
                {
                    //generate new key
                    MsgRequest.security.hsm_cmd = Message.Security.EnumHsmCommand.GenerateKey;

                    //send to hsm service
                    ErrCode = await _hsm.GenerateKey(MsgRequest);
                }
                else
                {
                    //send to hsm service
                    MsgRequest.security.hsm_cmd = Message.Security.EnumHsmCommand.TranslateKey;

                    //send to hsm service
                    ErrCode = await _hsm.TranslateKey(MsgRequest);
                }

                //construct response
                Message.Response MsgResponse = new(MsgRequest)
                {
                    resp_code = ErrCode.PadLeft(4, '0'),
                    resp_message = (ErrCode == HsmErrCode.SUCCESS ? "Success" : "Failed"),
                    authorized_by = AuthTran.INTERNAL
                };

                //reply
                await _nodes.SendToSource(MsgRequest.private_data.source_node, MsgResponse);
            }
            catch (Exception ex)
            {
                await Logger(ex.Message);
            }
        }

        private static async Task RequestKeyTerminal(Message.Request MsgRequest)
        {
            try
            {
                //generate new key
                MsgRequest.security.hsm_cmd = Message.Security.EnumHsmCommand.GenerateKeyTerminal;

                //send to hsm service
                string ErrCode = await _hsm.GenerateKeyTerminal(MsgRequest);

                //construct response
                Message.Response MsgResponse = new(MsgRequest)
                {
                    resp_code = ErrCode.PadLeft(4, '0'),
                    resp_message = (ErrCode == HsmErrCode.SUCCESS ? "Success" : "Failed"),
                    authorized_by = AuthTran.INTERNAL
                };

                //reply
                await _nodes.SendToSource(MsgRequest.private_data.source_node, MsgResponse);
            }
            catch (Exception ex)
            {
                await Logger(ex.Message);
            }
        }
        #endregion

        #region Command
        private async Task InitPortService()
        {
            int port = await DbMgr.GetAppPort();

            await _cmd.StartAsync(IPAddress.Any, port);

            //service command
            await Logger($"{APPNAME} command listening at port {port}");
        }

        private async Task RequestCommand(byte[] Data, EndPoint remoteEP)
        {
            try
            {
                string cmd = Encoding.UTF8.GetString(Data);
                string msg = "Receive command: " + cmd;
                string rsp = "OK";

                //log request
                await Logger(msg);

                switch (cmd.ToUpper())
                {
                    case "VERSION":
                        rsp = VERSION;
                        break;
                    case "RESYNC":
                        await _nodes.Resync();
                        await _hotcard.Resync();
                        await _route.Resync();
                        await _bsndate.Resync();

                        break;
                    case "TRACE OFF":
                        _IsTraceOn = false;
                        break;
                    case "TRACE ON":
                        _IsTraceOn = true;
                        break;
                    case "TRACE CLEAR":
                        rsp = "UNSUPPORTED COMMAND";
                        break;
                    default:
                        rsp = "UNKNOWN COMMAND";
                        break;
                }

                //reply
                await _cmd.SendToAsync(rsp, remoteEP);
            }
            catch (Exception ex)
            {
                await Logger(ex.Message);
            }
        }
        #endregion

        #region Reply
        private static async Task ReplyAndInsert(string SourceNode, Message.Request MsgRequest, string RC, string ErrorMessage)
        {
            try
            {
                //create object
                var MsgResponse = new Message.Response(MsgRequest)
                {
                    //set rc
                    resp_code = RC,
                    resp_message = ErrorMessage,
                    authorized_by = AuthTran.INTERNAL
                };

                //save to db
                await DbMgr.InsertTrxAuthInternal(MsgResponse);

                //send
                await _nodes.SendToSource(SourceNode, MsgResponse);
            }
            catch (Exception ex)
            {
                await Logger(ex.Message);
            }
        }

        private static async Task ReplyWithoutUpdate(string SourceNode, Message.Request MsgRequest, string RC, string ErrorMessage)
        {
            try
            {
                //create object
                var MsgResponse = new Message.Response(MsgRequest)
                {
                    //set rc
                    resp_code = RC,
                    resp_message = ErrorMessage,
                    authorized_by = AuthTran.INTERNAL
                };

                //send
                await _nodes.SendToSource(SourceNode, MsgResponse);
            }
            catch (Exception ex)
            {
                await Logger(ex.Message);
            }
        }

        private static async Task ReplyAndUpdate(string SourceNode, Message.Request MsgRequest, string RC, string ErrorMessage)
        {
            try
            {
                //create object
                var MsgResponse = new Message.Response(MsgRequest)
                {
                    //set rc
                    resp_code = RC,
                    resp_message = ErrorMessage,
                    authorized_by = AuthTran.INTERNAL
                };

                //save to db
                await DbMgr.UpdateTrx(MsgResponse);

                //send
                await _nodes.SendToSource(SourceNode, MsgResponse);
            }
            catch (Exception ex)
            {
                await Logger(ex.Message);
            }
        }

        private static async Task ReplyAutoReversal(string SourceNode, Message.Request MsgRequest)
        {
            try
            {
                string ErrorMessage;
                string RC;

                ReversalStatus rs = await DbMgr.GetRespCodeReversal(MsgRequest);

                if (rs == null)
                {
                    RC = RespCodeOct.RC63_PAYMENT_NOT_FOUND;
                    ErrorMessage = "Original request not found";
                }
                else if (rs.RespCodeReversal == RespCodeHost.RC00_SUCCESS)
                {
                    RC = RespCodeHost.RC00_SUCCESS;
                    ErrorMessage = "Success";
                }
                else if (string.IsNullOrEmpty(rs.RespCodeReversal) == true)
                {
                    RC = RespCodeOct.RC11_TRAN_PENDING;
                    ErrorMessage = "Pending";
                }
                else
                {
                    RC = rs.RespCodeReversal;
                    ErrorMessage = "Reversal failed";
                }

                //transaksi belum di reverse
                if (rs.TranReversed == "0")
                {
                    //kirim reversal ke host
                    await _nodes.SendToSink(MsgRequest.private_data.sink_node, MsgRequest);
                }
                else
                {
                    //create object
                    var MsgResponse = new Message.Response(MsgRequest)
                    {
                        //set rc
                        resp_code = RC,
                        resp_message = ErrorMessage,
                        authorized_by = AuthTran.INTERNAL
                    };

                    //update db
                    await DbMgr.UpdateTrx(MsgResponse);

                    //reply
                    await _nodes.SendToSource(SourceNode, MsgResponse);
                }
            }
            catch (Exception ex)
            {
                await Logger(ex.Message);
            }
        }
        #endregion
    }
}
