using ApiBiller.Common;
using ApiBiller.Constants;
using ApiBiller.Message;
using ApiBiller.Models;
using ApiBiller.Networking;
using Microsoft.AspNetCore.Http;
using SyncNet;
using SyncNet.Constants;
using SyncNet.Helpers;
using SyncNet.IsoMessage;
using SyncNet.Library;
using SyncNet.Message;
using SyncNet.Nodes;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ApiBiller
{
    class MyApp : IAppProcessor
    {
        public const string APPNAME = "API Biller";
        public const string VERSION = "v1.0";

        private readonly AppProcessor _app;
        private readonly INbCache _cache;

        public MyApp(INbCache cache)
        {
            _cache = cache;

            _app = new AppProcessor(APPNAME, VERSION, this);
        }

        public async Task Start(CancellationToken stoppingToken)
        {
            try
            {
                //read config
                ApiConfig.Initialize();

                //start app harus paling akhir
                await _app.AppStart(stoppingToken);
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
            await _app.AppStop();
        }

        public async Task Resync()
        {
            //read config
            ApiConfig.Initialize();
        }

        public async Task OnError(string NodeName, string ConnectionName, Request MsgOriginal, string ErrorMessage)
        {
            await Logger(NodeName, ErrorMessage, "");
        }

        public async Task ProcessMsgFromSinkNode(string NodeName, Request MsgRequest)
        {
            try
            {
                //check connection
                if (ApiConfig.NonPersistent == true && _app.IsConnected(NodeName) == false)
                {
                    //link down
                    await ReplyTransactionLinkDown(NodeName, MsgRequest);
                    return;
                }

                var isoRmt = new Iso8583(new IsoTemplate());

                switch (MsgRequest.tran_type)
                {
                    case TranType.INQUIRY:
                        isoRmt = ToRemote.Inquiry(MsgRequest);
                        break;
                    case TranType.PAYMENT:
                        isoRmt = ToRemote.Payment(MsgRequest);
                        break;
                    case TranType.ADVICE:
                        isoRmt = ToRemote.Advice(MsgRequest);
                        break;
                    case TranType.REVERSAL:
                        isoRmt = ToRemote.Reversal(MsgRequest);
                        break;

                    default:
                        //transaction unsupported
                        await ReplyTransactionUnsupported(NodeName, MsgRequest);
                        return;
                }

                //save original message using unique key
                string key = string.Concat(isoRmt.MsgType.AsSpan(0, 2), 
                    isoRmt.GetField(11), isoRmt.GetField(41));

                //add data to buffer
                _cache.Add(key, new CacheModel { msg = MsgRequest });

                //packing iso message
                byte[] bytes = isoRmt.Pack();

                //get node parameter
                if (_app.GetNode(NodeName, out NodeRemote node) == false)
                {
                    await Logger(NodeName, $"Send to remote, node {NodeName} not found", "");
                    return;
                }

                //log outgoing
                if (AppProcessor.IsTraceOn() == true)
                {
                    await _app.WriteTrace(NodeName, isoRmt.MsgType,
                        isoRmt.GetFormattedSimple(),
                        AppProcessor.EnumFromTo.To, node.GetEndPointClient());
                }

                //connection type
                if (ApiConfig.NonPersistent == true)
                {
                    //non persistent
                    await SendToTcpNonPersistent(NodeName, node, bytes, MsgRequest);
                }
                else
                {
                    //persistent
                    await _app.SendToTcp(NodeName, bytes);
                }
            }
            catch (Exception ex)
            {
                await Logger(NodeName, ex.Message, "");
            }

            return;
        }

        public Task ProcessMsgFromSourceNode(string NodeName, string ConnectionName, Response MsgResponse)
        {
            throw new NotImplementedException();
        }

        public async Task ProcessMsgFromRemoteTcp(string NodeName, string ConnectionName, byte[] Bytes, int TotalBytes, EndPoint RemoteEP)
        {
            try
            {
                //unpack message from remote
                var iso = new Iso8583(new IsoTemplate());
                if (iso.Unpack(Bytes) != 0)
                {
                    await Logger(NodeName, iso.GetLastError(), NbFormat.FormatBinary(Bytes));
                    return;
                }

                //log incoming
                if (AppProcessor.IsTraceOn() == true)
                {
                    if (iso.MsgType == "0810" && ApiConfig.LogEchoTest == false)
                    {
                        //do not log trace
                    }
                    else
                    {
                        await _app.WriteTrace(NodeName, iso.MsgType,
                            iso.GetFormattedSimple(),
                            AppProcessor.EnumFromTo.From,
                            NetHelper.GetRemoteEP(RemoteEP));
                    }
                }

                //check network management response
                if (iso.MsgType == "0810") return;

                //unique key to locate original message
                string key = string.Concat(iso.MsgType.AsSpan(0, 2), 
                    iso.GetField(11), iso.GetField(41));

                //get original message from cache
                if (_cache.GetValue(key, out CacheModel obj) == false)
                {
                    await Logger(NodeName, $"Unable to locate original transaction {key}",
                        NbFormat.FormatBinary(Bytes));

                    return;
                }

                //original message
                Request msgReq = obj.msg;

                //message to internal
                Response msgRsp = null;

                switch (msgReq.tran_type)
                {
                    case TranType.INQUIRY:
                        msgRsp = ToInternal.Inquiry(iso, msgReq);
                        break;
                    case TranType.PAYMENT:
                        msgRsp = ToInternal.Payment(iso, msgReq);
                        break;
                    case TranType.ADVICE:
                        msgRsp = ToInternal.Advice(iso, msgReq);
                        break;
                    case TranType.REVERSAL:
                        msgRsp = ToInternal.Reversal(iso, msgReq);
                        break;
                }

                //reply to middleware
                if (msgRsp == null)
                {
                    await Logger(NodeName, $"Ignore reply from remote",
                        iso.GetFormattedSimple() + NbFormat.FormatBinary(Bytes));

                    return;
                }

                //reply
                await _app.ReplyToSink(NodeName, msgRsp);
            }
            catch (Exception ex)
            {
                await Logger(NodeName, ex.Message, NbFormat.FormatBinary(Bytes));
            }
        }

        public Task ProcessMsgFromRemoteWsServer(string NodeName, string ConnectionName, string WSMessage, HttpContext Ctx)
        {
            throw new NotImplementedException();
        }

        public Task ProcessMsgFromRemoteWsClient(string NodeName, string ConnectionName, Request MsgOriginal, string WSMessage, HttpStatusCode StatusCode)
        {
            throw new NotImplementedException();
        }

        public async Task TimerAutoSignon(string NodeName)
        {
            await SendNetworkManagement(NodeName, NtwrkMgmtCode.LOGON);
        }

        public async Task TimerEcho(string NodeName)
        {
            await SendNetworkManagement(NodeName, NtwrkMgmtCode.ECHO_TEST);
        }

        public async Task TimerKeyExchange(string NodeName)
        {
            throw new NotImplementedException();
        }

        public async Task NetworkManagement(AppProcessor.EnumNtwrkMgmt cmd, string NodeName, string Param)
        {
            switch (cmd)
            {
                case AppProcessor.EnumNtwrkMgmt.Echo:
                    await SendNetworkManagement(NodeName, NtwrkMgmtCode.ECHO_TEST);
                    break;
                case AppProcessor.EnumNtwrkMgmt.SignOn:
                    await SendNetworkManagement(NodeName, NtwrkMgmtCode.LOGON);
                    break;
                case AppProcessor.EnumNtwrkMgmt.SignOff:
                    await SendNetworkManagement(NodeName, NtwrkMgmtCode.LOGOFF);
                    break;
            }
        }

        private async Task ReplyTransactionUnsupported(string NodeName, Request MsgRequest)
        {
            var rsp = new Response(MsgRequest)
            {
                resp_code = "A1",
                resp_message = "Transaction is not supported",
                authorized_by = AuthTran.INTERNAL
            };

            //send to internal
            await _app.ReplyToSink(NodeName, rsp);
        }

        private async Task ReplyTransactionLinkDown(string NodeName, Request MsgRequest)
        {
            var rsp = new Response(MsgRequest)
            {
                resp_code = "89",
                resp_message = "Link down",
                authorized_by = AuthTran.INTERNAL
            };

            //send to internal
            await _app.ReplyToSink(NodeName, rsp);
        }

        private async Task SendNetworkManagement(string NodeName, string Code)
        {
            Iso8583 iso = ToRemote.NtwrkMgmt(Code);
            byte[] bytes = iso.Pack();

            string ep = "";
            if (_app.GetNode(NodeName, out NodeRemote node) == true)
                ep = node.GetEndPointClient();

            //log
            if (AppProcessor.IsTraceOn() == true)
            {
                if (Code == NtwrkMgmtCode.ECHO_TEST && ApiConfig.LogEchoTest == false)
                {
                    //do not log trace
                }
                else
                {
                    await _app.WriteTrace(NodeName, iso.MsgType, iso.GetFormattedSimple(),
                        AppProcessor.EnumFromTo.To, ep);
                }
            }

            //send to remote
            if (ApiConfig.NonPersistent == true)
                await SendToTcpNonPersistent(NodeName, node, bytes, null);
            else
                await _app.SendToTcp(NodeName, bytes);
        }

        private async Task SendToTcpNonPersistent(string NodeName, NodeRemote node, byte[] data, Request MsgRequest)
        {
            string RemoteHost = node.RemoteHost;
            int RemotePort = node.RemotePort;

            try
            {
                var tcpClientHandler = new TcpClientHandler(RemoteHost, RemotePort);

                //check connecction
                if (tcpClientHandler.IsConnected() == false)
                {
                    await Logger(NodeName, $"Unable to connect to remote host {RemoteHost},{RemotePort} trace number {MsgRequest.trace_number}", "");
                    return;
                }

                // Send data
                await tcpClientHandler.SendDataAsync(data);

                // Waiting for the response from the remote host
                byte[] response = await tcpClientHandler.ReceiveDataAsync();

                // Close the connection
                tcpClientHandler.Close();

                //network management
                if (MsgRequest == null) return;

                //call process response from remote
                if (response != null)
                {
                    EndPoint ep = NetHelper.GetEndPoint(RemoteHost, RemotePort);
                    await ProcessMsgFromRemoteTcp(NodeName, node.ConnectionName, response, response.Length, ep);
                }
            }
            catch (Exception ex)
            {
                await Logger(NodeName, ex.Message, NbFormat.FormatBinary(data));
            }
        }

        private async Task Logger(string NodeName, string Info, string Detail)
        {
            //log
            await AppProcessor.Logger(Info, Detail, NodeName);

            //debug
            //Console.WriteLine($"NodeName={NodeName},Info={Info},Detail={Detail}");
        }
    }
}
