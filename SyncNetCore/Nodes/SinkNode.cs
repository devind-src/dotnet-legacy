using Newtonsoft.Json;
using SyncNet.Constants;
using SyncNet.Helpers;
using SyncNet.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet.Nodes
{
    class SinkNode
    {
        //variable
        public string NodeName;
        public string InstID;
        public string AutoReversal;
        public string ProviderService;
        public string AuthService;
        public string Issuer;
        public string ProtectSensitiveData;
        public string CalendarName;
        public string SendCutover;
        public string SaveRepeatReversal;

        public int Port;
        public int RequestTimeout;
        public int AdviceTimeout;
        public int MaxRetrySend;

        private readonly ConcurrentDictionary<string, NodeData> _buf;

        private readonly XTcpListener _socket;

        private Timer _tmr;

        private bool _ischeck;

        public SinkNode()
        {
            _buf = new ConcurrentDictionary<string, NodeData>();

            _socket = new XTcpListener();
            _socket.OnDataArrival += SocketOnDataArrival;
            _socket.OnConnect += SocketOnConnect;
            _socket.OnDisconnect += SocketOnDisconnect;
            _socket.OnError += SocketOnError;
        }

        public async Task Start()
        {
            try
            {
                await MyApp.Logger($"{this.NodeName} sink listening at {this.Port}");

                await _socket.StartAsync(IPAddress.Any, this.Port);

                //start timer
                _tmr = new Timer(new TimerCallback(CheckMsgTimeout), null, 0, 100);
            }
            catch (Exception ex)
            {
                await MyApp.Logger(ex.Message);
            }
        }

        public async Task Stop()
        {
            await MyApp.Logger($"{this.NodeName} sink connection close");

            //disconnect
            await _socket.StopAsync();

            //clear buffer
            _buf.Clear();

            //stop timer
            _tmr.Dispose();
        }

        public async Task Send(Message.Request MsgRequest)
        {
            if (MyApp.IsTraceOn() == true)
            {
                //check pcidss
                if (ProtectSensitiveData == "1")
                {
                    //masking
                    MsgRequest.pan = DataHelper.GetMasking(MsgRequest.pan);
                    MsgRequest.security.track2data = DataHelper.GetMasking(MsgRequest.security.track2data);

                    //construct new trace
                    string trace = JsonConvert.SerializeObject(MsgRequest, Formatting.None);

                    //log outgoing
                    await MyApp.LogTraceOutgoing("REQ", trace, NodeName, $"127.0.0.1,{Port}");
                }
                else
                {
                    //construct new trace
                    string trace = JsonConvert.SerializeObject(MsgRequest, Formatting.None);

                    //log outgoing
                    await MyApp.LogTraceOutgoing("REQ", trace, NodeName, $"127.0.0.1,{Port}");
                }
            }

            //get json
            string WSMessage = JsonConvert.SerializeObject(MsgRequest);

            //sink is not connected
            if (_socket.IsConnected() == false)
            {
                //create response
                var MsgResponse = new Message.Response(MsgRequest)
                {
                    resp_code = RespCodeOct.RC91_LINK_DOWN,
                    resp_message = "Link down",
                    authorized_by = AuthTran.INTERNAL
                };

                //reply message
                await MyApp.OnDataArrivalSinkAsync(this.NodeName, MsgResponse);
                return;
            }

            //get key
            string key = NodeName + DataHelper.GetSwitchKey(MsgRequest);

            //create object
            var obj = new NodeData
            {
                MsgRequest = MsgRequest
            };

            //set timeout
            if (MsgRequest.tran_type == TranType.ADVICE ||
                MsgRequest.tran_type == TranType.REVERSAL)
                obj.MaxExpired = AdviceTimeout;
            else
                obj.MaxExpired = RequestTimeout;

            //add to buffer failed
            if (_buf.TryAdd(key, obj) == false)
            {
                //create response
                var MsgResponse = new Message.Response(MsgRequest)
                {
                    resp_code = RespCodeOct.RC94_DUPLICATE,
                    resp_message = "Duplicate transaction",
                    authorized_by = AuthTran.INTERNAL
                };

                //reply message
                await MyApp.OnDataArrivalSinkAsync(this.NodeName, MsgResponse);
                return;
            }

            //send
            await _socket.SendAsync(WSMessage);
        }

        private void CheckMsgTimeout(object state)
        {
            try
            {
                if (_ischeck == true) return; else _ischeck = true;

                var k = new List<string>();

                //check buffer expired
                foreach (var pair in _buf)
                {
                    if (pair.Value.IsExpired() == true)
                    {
                        k.Add(pair.Key);
                    }
                }

                //remove from buffer
                foreach (string s in k)
                {
                    if (_buf.TryRemove(s, out NodeData obj) == true)
                    {
                        //auto reversal/advice menggunakan setting sink node
                        obj.MsgRequest.private_data.mode_timeout = AutoReversal;
                        obj.MsgRequest.private_data.max_retry_send = MaxRetrySend;
                        obj.MsgRequest.private_data.save_repeat_reversal = SaveRepeatReversal;

                        //set original data
                        if (obj.MsgRequest.tran_type != TranType.REVERSAL &&
                            obj.MsgRequest.tran_type != TranType.ADVICE)
                        {
                            obj.MsgRequest.original_data = obj.MsgRequest.tran_type.ToUpper() +
                                obj.MsgRequest.datetime_tran + obj.MsgRequest.trace_number;
                        }

                        //reversal & advice from external
                        if ((obj.MsgRequest.tran_type == TranType.REVERSAL ||
                            obj.MsgRequest.tran_type == TranType.ADVICE) &&
                            obj.MsgRequest.private_data.is_req_internal == false)
                        {
                            //no need to repeat
                        }
                        else
                        {
                            //pooling to transaction manager
                            MyApp.OnMessageTimeout(this.NodeName, obj.MsgRequest);
                        }
                    }
                }
            }
            catch { }
            finally
            {
                //check finish
                _ischeck = false;
            }
        }

        private async Task SocketOnDataArrival(byte[] Data, EndPoint remoteEP)
        {
            //get string
            string WSMessage = Encoding.UTF8.GetString(Data);

            //extract message response
            Message.Response MsgResponse =
                JsonConvert.DeserializeObject<Message.Response>(WSMessage);

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
                    await MyApp.LogTraceIncoming("RSP", trace, NodeName, ep);
                }
                else
                {
                    //formated
                    string trace = JsonConvert.SerializeObject(MsgResponse, Formatting.None);

                    //log incoming
                    await MyApp.LogTraceIncoming("RSP", trace, NodeName, ep);
                }
            }

            //check original transaction
            string key = NodeName + DataHelper.GetSwitchKey(MsgResponse);

            //get request from buffer
            if (_buf.TryGetValue(key, out NodeData obj) == true)
            {
                //copy original request
                MsgResponse.echo_data = obj.MsgRequest.echo_data;
                MsgResponse.original_data = obj.MsgRequest.original_data;
                MsgResponse.private_data = obj.MsgRequest.private_data;
                MsgResponse.fee_data = obj.MsgRequest.fee_data;
                MsgResponse.virtual_account = obj.MsgRequest.virtual_account;

                //pooling to transaction manager
                await MyApp.OnDataArrivalSinkAsync(this.NodeName, MsgResponse);

                //remove from buffer
                _buf.TryRemove(key, out _);
            }
            else
            {
                //logger
                await MyApp.Logger("Original transaction not found", WSMessage);
            }
        }

        private async Task SocketOnDisconnect(EndPoint remoteEP)
        {
            //logger
            await MyApp.Logger($"{NodeName} sink disconnected from {NetHelper.GetRemoteEP(remoteEP)}");
        }

        private async Task SocketOnConnect(EndPoint remoteEP)
        {
            //logger
            await MyApp.Logger($"{NodeName} sink connected to {NetHelper.GetRemoteEP(remoteEP)}");
        }

        private async Task SocketOnError(Exception ex, EndPoint ep)
        {
            // Abaikan error yang sifatnya expected / non-kritis
            if (ex is OperationCanceledException || ex is TaskCanceledException)
                return;

            //logger
            await MyApp.Logger($"{NodeName} sink error: {ex.Message}");
        }
    }
}
