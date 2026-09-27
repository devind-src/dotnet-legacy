using Newtonsoft.Json;
using SyncNet.Helpers;
using SyncNet.Networking;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SyncNet.Nodes
{
    class SourceNode
    {
        //variable
        public string NodeName;
        public string InstID;
        public string AutoReversal;
        public string AutoReplyReversal;
        public int Port;

        public int EnableClosing;
        public string ClosingTimeStart;
        public string ClosingTimeEnd;
        public string CalendarName;

        public string PinTranslate;
        public string ProtectSensitiveData;

        private readonly XTcpListener _socket;

        public SourceNode()
        {
            _socket = new XTcpListener();
            _socket.OnConnect += SocketOnConnect;
            _socket.OnDisconnect += SocketOnDisconnect;
            _socket.OnDataArrival += SocketOnDataArrival;
            _socket.OnError += SocketOnError;
        }

        public void SetConfig(ref Message.Request MsgRequest, string ep)
        {
            //request from remote
            MsgRequest.private_data.is_req_internal = false;

            //connection
            MsgRequest.private_data.source_node = this.NodeName;
            MsgRequest.private_data.ip_source = ep;

            //business calendar
            MsgRequest.private_data.enable_closing = this.EnableClosing;
            MsgRequest.private_data.closing_time_start = this.ClosingTimeStart;
            MsgRequest.private_data.closing_time_end = this.ClosingTimeEnd;
            MsgRequest.private_data.calendar_name = this.CalendarName;

            //mode timeout
            MsgRequest.private_data.mode_timeout = AutoReversal;
            MsgRequest.private_data.auto_reply_rev = AutoReplyReversal;

            //security
            MsgRequest.security.pin_translate = this.PinTranslate;
            MsgRequest.security.protect_sensitive_data = this.ProtectSensitiveData;
        }

        public async Task Start()
        {
            try
            {
                await MyApp.Logger($"{this.NodeName} source listening at {this.Port}");

                //start socket
                await _socket.StartAsync(IPAddress.Any, this.Port);
            }
            catch (Exception ex)
            {
                await MyApp.Logger(ex.Message);
            }
        }

        public async Task Stop()
        {
            await MyApp.Logger($"{this.NodeName} source connection close");

            //stop socket
            await _socket.StopAsync();
        }

        public async Task Send(Message.Response MsgResponse)
        {
            string ip_source = MsgResponse.private_data.ip_source;

            //get original ip source
            EndPoint ep = NetHelper.GetEndPoint(ip_source);

            if (MyApp.IsTraceOn() == true)
            {
                //check pcidss
                if (ProtectSensitiveData == "1")
                {
                    //masking
                    MsgResponse.pan = DataHelper.GetMasking(MsgResponse.pan);
                    MsgResponse.security.track2data = DataHelper.GetMasking(MsgResponse.security.track2data);

                    //construct new trace
                    string trace = JsonConvert.SerializeObject(MsgResponse, Formatting.None);

                    //log outgoing
                    await MyApp.LogTraceOutgoing("RSP", trace, NodeName, ip_source);
                }
                else
                {
                    //construct new trace
                    string trace = JsonConvert.SerializeObject(MsgResponse, Formatting.None);

                    //log outgoing
                    await MyApp.LogTraceOutgoing("RSP", trace, NodeName, ip_source);
                }
            }

            //get json
            string WSMessage = JsonConvert.SerializeObject(MsgResponse);

            //send
            await _socket.SendToAsync(WSMessage, ep);
        }

        private async Task SocketOnDataArrival(byte[] Data, EndPoint remoteEP)
        {
            //get string
            string WSMessage = Encoding.UTF8.GetString(Data);

            //extract message
            Message.Request MsgRequest =
                JsonConvert.DeserializeObject<Message.Request>(WSMessage);

            //get remote address
            string ep = NetHelper.GetRemoteEP(remoteEP);

            if (MyApp.IsTraceOn() == true)
            {
                //check pcidss
                if (ProtectSensitiveData == "1")
                {
                    //extract message
                    Message.Request req =
                        JsonConvert.DeserializeObject<Message.Request>(WSMessage);

                    //masking
                    req.pan = DataHelper.GetMasking(req.pan);
                    req.security.track2data = DataHelper.GetMasking(req.security.track2data);

                    //construct new trace
                    string trace = JsonConvert.SerializeObject(req, Formatting.None);

                    //log incoming
                    await MyApp.LogTraceIncoming("REQ", trace, NodeName, ep);
                }
                else
                {
                    //formated message
                    string trace = JsonConvert.SerializeObject(MsgRequest, Formatting.None);

                    //log incoming
                    await MyApp.LogTraceIncoming("REQ", trace, NodeName, ep);
                }
            }

            //Add config to message
            SetConfig(ref MsgRequest, ep);

            //pooling to transaction manager
            await MyApp.OnDataArrivalSourceAsync(this.NodeName, MsgRequest);
        }

        private async Task SocketOnDisconnect(EndPoint endPoint)
        {
            //logger
            await MyApp.Logger($"{NodeName} source disconnected from {NetHelper.GetRemoteEP(endPoint)}");
        }

        private async Task SocketOnConnect(EndPoint endPoint)
        {
            //logger
            await MyApp.Logger($"{NodeName} source connected to {NetHelper.GetRemoteEP(endPoint)}");
        }

        private async Task SocketOnError(Exception ex, EndPoint ep)
        {
            // Abaikan error yang sifatnya expected / non-kritis
            if (ex is OperationCanceledException || ex is TaskCanceledException)
                return;

            //logger
            await MyApp.Logger($"{NodeName} source error: {ex.Message}");
        }
    }
}
