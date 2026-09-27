using ApiBiller.Common;
using ApiBiller.Message;
using ApiBiller.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SyncNet;
using SyncNet.Constants;
using SyncNet.Cryptography;
using SyncNet.Message;
using SyncNet.Networking;
using SyncNet.Nodes;
using System;
using System.Collections.Generic;
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

        public MyApp()
        {
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
                TransactionModel.Request msgToRemote = null;
                string rawUrl = string.Empty;

                switch (MsgRequest.tran_type)
                {
                    case TranType.INQUIRY:
                        rawUrl = "/bill/inquiry";
                        msgToRemote = ToRemote.Inquiry(MsgRequest);
                        break;
                    case TranType.PAYMENT:
                        rawUrl = "/bill/payment";
                        msgToRemote = ToRemote.Payment(MsgRequest);
                        break;
                    case TranType.ADVICE:
                        rawUrl = "/bill/advice";
                        msgToRemote = ToRemote.Advice(MsgRequest);
                        break;
                    case TranType.REVERSAL:
                        rawUrl = "/bill/reversal";
                        msgToRemote = ToRemote.Reversal(MsgRequest);
                        break;

                    default:
                        //transaction unsupported
                        await ReplyTransactionUnsupported(NodeName, MsgRequest);
                        return;
                }

                //get node parameter
                if (_app.GetNode(NodeName, out NodeRemote node) == false)
                {
                    await Logger(NodeName, $"Send to remote, node {NodeName} not found", "");
                    return;
                }

                //set url: per node dari sw_connections.ws_url (tiap biller bisa beda host/port),
                //Urls:UrlBase di appsettings hanya dipakai bila ws_url node kosong
                string urlBase = string.IsNullOrWhiteSpace(node.WSUrl) == false ? node.WSUrl : ApiConfig.UrlBase;
                string url = urlBase.TrimEnd('/') + rawUrl;

                //serialize to json
                string json = JsonConvert.SerializeObject(msgToRemote);

                //log outgoing
                if (AppProcessor.IsTraceOn() == true)
                {
                    await _app.WriteTrace(NodeName, "REQ", json,
                        AppProcessor.EnumFromTo.To, url);
                }

                //send to remote default simple
                //await _app.SendToHttpClient(NodeName, MsgRequest, json, null);

                //send to remote custom
                await SendToHttpCustom(NodeName, MsgRequest, json, url);
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

        public Task ProcessMsgFromRemoteTcp(string NodeName, string ConnectionName, byte[] Bytes, int TotalBytes, EndPoint RemoteEP)
        {
            throw new NotImplementedException();
        }

        public Task ProcessMsgFromRemoteWsServer(string NodeName, string ConnectionName, string WSMessage, HttpContext Ctx)
        {
            throw new NotImplementedException();
        }

        public async Task ProcessMsgFromRemoteWsClient(string NodeName, string ConnectionName, Request MsgOriginal, string WSMessage, HttpStatusCode StatusCode)
        {
            try
            {
                //log incoming
                if (AppProcessor.IsTraceOn() == true)
                {
                    //get node parameter
                    _app.GetNode(NodeName, out NodeRemote node);

                    await _app.WriteTrace(NodeName, "RSP", WSMessage,
                        AppProcessor.EnumFromTo.From, $"{node?.WSUrl} [{StatusCode}]");
                }

                //validate message
                if (string.IsNullOrEmpty(WSMessage)) return;

                //deserialize message
                var msgResponse = JsonConvert.DeserializeObject<TransactionModel.Response>(WSMessage);

                //message to internal
                Response msgRsp = null;

                switch (MsgOriginal.tran_type)
                {
                    case TranType.INQUIRY:
                        msgRsp = ToInternal.Inquiry(msgResponse, MsgOriginal);
                        break;
                    case TranType.PAYMENT:
                        msgRsp = ToInternal.Payment(msgResponse, MsgOriginal);
                        break;
                    case TranType.ADVICE:
                        msgRsp = ToInternal.Advice(msgResponse, MsgOriginal);
                        break;
                    case TranType.REVERSAL:
                        msgRsp = ToInternal.Reversal(msgResponse, MsgOriginal);
                        break;
                }

                //reply to middleware
                if (msgRsp == null)
                {
                    await Logger(NodeName, $"Ignore reply from remote", WSMessage);

                    return;
                }

                //reply
                await _app.ReplyToSink(NodeName, msgRsp);
            }
            catch (Exception ex)
            {
                await Logger(NodeName, ex.Message, WSMessage);
            }
        }

        public async Task TimerAutoSignon(string NodeName)
        {
            throw new NotImplementedException();
        }

        public async Task TimerEcho(string NodeName)
        {
            throw new NotImplementedException();
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
                case AppProcessor.EnumNtwrkMgmt.SignOn:
                case AppProcessor.EnumNtwrkMgmt.SignOff:
                case AppProcessor.EnumNtwrkMgmt.KeyChange:
                    break;
            }
        }


        private async Task SendToHttpCustom(string NodeName, Request MsgOriginal, string MsgToRemote, string Url)
        {
            //calculate signature
            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string msgToHash = $"{ApiConfig.ClientId}:{dateTime}:{MsgToRemote}";
            byte[] bytes = HashProvider.ComputeHMACSHA512Hash(msgToHash, ApiConfig.SecretKey);
            string signature = Convert.ToBase64String(bytes);

            //add headers
            var headers = new Dictionary<string, string>
            {
                { "X-DateTime", dateTime },
                { "X-ClientId", ApiConfig.ClientId },
                { "X-Signature", signature }
            };

            //send to remote
            var res = await XHttpClient.PostAsync(Url, MsgToRemote, headers,
                ApiConfig.TimeoutSeconds);

            //koneksi ke biller ditolak / host tidak terjangkau: request pasti belum terkirim,
            //balas link down supaya core tidak menunggu sampai timeout. timeout atau koneksi putus
            //di tengah tetap tanpa balasan (status transaksi tidak pasti, ditangani timeout core).
            if (res.StatusCode == HttpStatusCode.BadGateway)
            {
                await Logger(NodeName, $"Send to remote failed: {res.MsgError}", Url);
                await ReplyLinkDown(NodeName, MsgOriginal, res.MsgError);
                return;
            }

            //response
            await ProcessMsgFromRemoteWsClient(NodeName, "", MsgOriginal, res.MsgResponse, res.StatusCode);
        }

        private async Task ReplyLinkDown(string NodeName, Request MsgRequest, string Error)
        {
            var rsp = new Response(MsgRequest)
            {
                resp_code = RC_LINK_DOWN,
                resp_message = $"Link down: {Error}",
                authorized_by = AuthTran.INTERNAL
            };

            //send to internal
            await _app.ReplyToSink(NodeName, rsp);
        }

        private const string RC_LINK_DOWN = "91";

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

        private async Task Logger(string NodeName, string Info, string Detail)
        {
            //log
            await AppProcessor.Logger(Info, Detail, NodeName);

            //debug
            //Console.WriteLine($"NodeName={NodeName},Info={Info},Detail={Detail}");
        }
    }
}
