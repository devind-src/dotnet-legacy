using Microsoft.AspNetCore.Http;
using SyncNet.Common;
using SyncNet.Constants;
using SyncNet.DbRepository;
using SyncNet.Library;
using SyncNet.Models;
using SyncNet.Networking;
using SyncNet.Nodes;
using SyncNet.Services;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet
{
    public class AppProcessor
    {
        //constanta
        public const string APPNAME = "SyncNetSdk";

        public const string CMD_VERSION = "VERSION";
        public const string CMD_RESYNC = "RESYNC";

        public const string CMD_ECHO = "ECHO";
        public const string CMD_SIGNON = "SIGNON";
        public const string CMD_SIGNOFF = "SIGNOFF";
        public const string CMD_KEYCHANGE = "KEYCHANGE";
        public const string CMD_OTHER = "OTHER";

        public const string CMD_TRACE_ON = "TRACE ON";
        public const string CMD_TRACE_OFF = "TRACE OFF";
        public const string CMD_TRACE_CLEAR = "TRACE CLEAR";

        //enum
        public enum EnumStatus { Off = 0, On = 1 }
        public enum EnumFromTo { From = 0, To = 1 }
        public enum EnumNtwrkMgmt { Echo = 0, SignOn = 1, SignOff = 2, KeyChange = 3, Other = 4 }


        //variable
        private static IAppProcessor _iapp;

        private static XTcpListenerSdk _cmd;
        private static LogService _logService;
        private static NbLogger _logger;

        private readonly NodeRemotes _nodeRmt;
        private readonly NodeInternal _nodeInt;

        private static DbMgr _dbMgr;

        private static string _appname;
        private readonly string _version;

        private static bool _isTraceOn = true;

        public AppProcessor(string AppName, string Version, IAppProcessor ApplicationProcessor)
        {
            _iapp = ApplicationProcessor;
            _appname = AppName;
            _version = Version;

            //init config
            SdkConfig.Initialize();

            _logger = new NbLogger(AppName);
            _dbMgr = new DbMgr();

            _nodeRmt = new NodeRemotes(AppName);
            _nodeInt = new NodeInternal();

            //command
            _cmd = new XTcpListenerSdk();
            _cmd.OnDataArrival += RequestCommand;

            //log services
            _logService = new LogService();

            //register provider EBCDIC dll
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public async Task AppStart(CancellationToken cancellationToken)
        {
            try
            {
                //get port logger
                //var ep = await _dbMgr.GetEndPointLogServices();

                //start log services
                await _logService.StartAsync(cancellationToken);

                //logger
                await Logger($"{_appname} started");

                //app started
                await _dbMgr.UpdateApp(_appname, DbMgr.EnumStatusApp.UP);
                await _dbMgr.UpdateNodes(_appname, DbMgr.EnumStatusApp.UP);

                //get port command
                int port = await _dbMgr.GetPortCommand(_appname);

                //load source & sink node
                await _nodeInt.Start(_appname);

                //load interchange
                await _nodeRmt.Start();

                //logger
                await Logger($"{_appname} command listening at port {port}");

                //start commander
                await _cmd.StartAsync(IPAddress.Any, port, cancellationToken);

                // JAGA AGAR APLIKASI TIDAK CLOSING
                // Task.Delay(-1) akan menunggu selamanya tanpa memakan CPU (non-blocking)
                await Task.Delay(-1, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                //log & trace
                await Logger($"{_appname} shutdown");
            }
            catch (Exception ex)
            {
                //log only
                await _logger.LogAsync(ex.Message);
            }
        }

        public async Task AppStop()
        {
            try
            {
                //logger
                await Logger($"{_appname} stop");

                //app down
                await _dbMgr.UpdateApp(_appname, DbMgr.EnumStatusApp.DOWN);
                await _dbMgr.UpdateNodes(_appname, DbMgr.EnumStatusApp.DOWN);

                //release resource
                await _nodeRmt.Stop();
                await _nodeInt.Stop();

                //stop commander
                await _cmd.StopAsync();

                //stop log service
                await _logService.StopAsync();
            }
            catch (Exception) { }
        }

        public bool GetNode(string NodeName, out NodeRemote Node)
        {
            return _nodeRmt.GetNode(NodeName, out Node);
        }

        public bool GetNode(string NodeName, string ConnName, out NodeRemote Node)
        {
            return _nodeRmt.GetNode(NodeName, ConnName, out Node);
        }

        public NodeRemote GetNodeRemote(string NodeName)
        {
            return _nodeRmt.GetNodeRemote(NodeName);
        }

        public NodeRemote GetNodeRemote(string NodeName, string ConnName)
        {
            return _nodeRmt.GetNodeRemote(NodeName, ConnName);
        }

        public bool IsConnected(string NodeName)
        {
            return _nodeRmt.IsConnected(NodeName);
        }

        public bool IsConnected(string NodeName, string ConnName)
        {
            return _nodeRmt.IsConnected(NodeName, ConnName);
        }

        public async Task Resync()
        {
            try
            {
                //resync interchange
                await _nodeRmt.Resync();

                //resync source & sink node
                await _nodeInt.Resync(_appname);
            }
            catch (Exception ex)
            {
                //log only
                await _logger.LogAsync(ex.Message);
            }
        }

        public async Task ResetTcp(string NodeName)
        {
            await _nodeRmt.ResetTcp(NodeName);
        }

        public async Task ReplyToTcp(string NodeName, string ConnName, byte[] Bytes, EndPoint EP)
        {
            await _nodeRmt.TcpReply(NodeName, ConnName, Bytes, EP);
        }

        public async Task ReplyToHttpServer(string NodeName, string ConnName, string MsgResponse, HttpContext Ctx)
        {
            await _nodeRmt.HttpServerReply(NodeName, ConnName, MsgResponse, Ctx);
        }

        public async Task ReplyToHttpServer(string NodeName, string ConnName, HttpStatusCode Code, HttpContext Ctx)
        {
            await _nodeRmt.HttpServerReply(NodeName, ConnName, Code, Ctx);
        }

        public async Task ReplyToSink(string NodeName, Message.Response MsgResponse)
        {
            await _nodeInt.SendToSink(NodeName, MsgResponse);
        }

        public async Task SendToSource(string NodeName, string ConnName, HttpContext Ctx, Message.Request MsgRequest)
        {
            //HttpListenerContext tdk bisa di serialize ke json !!!

            //set connection name for reply
            MsgRequest.private_data.connection_name = ConnName;
            MsgRequest.private_data.ip_external = Ctx.Connection.RemoteIpAddress.ToString();

            await _nodeInt.SendToSource(NodeName, MsgRequest);
        }

        public async Task SendToSource(string NodeName, string ConnName, string IPEndPoint, Message.Request MsgRequest)
        {
            //set connection name for reply
            MsgRequest.private_data.connection_name = ConnName;
            MsgRequest.private_data.ip_external = IPEndPoint;

            await _nodeInt.SendToSource(NodeName, MsgRequest);
        }

        public async Task SendToTcp(string NodeName, byte[] Bytes)
        {
            await _nodeRmt.TcpSend(NodeName, Bytes);
        }

        public async Task SendToTcp(string NodeName, string ConnName, byte[] Bytes)
        {
            await _nodeRmt.TcpSend(NodeName, ConnName, Bytes);
        }

        public async Task SendToHttpClient(string NodeName, Message.Request MsgOriginal, string MsgRequest, WebHeaderCollection Header)
        {
            await _nodeRmt.HttpClientSend(NodeName, MsgOriginal, MsgRequest, Header);
        }

        public async Task SendToHttpClient(string NodeName, Message.Request MsgOriginal, string MsgRequest, string Parameter, WebHeaderCollection Header)
        {
            await _nodeRmt.HttpClientSend(NodeName, MsgOriginal, MsgRequest, Parameter, Header);
        }

        public static void SetTrace(EnumStatus state)
        {
            if (state == EnumStatus.On)
                _isTraceOn = true;
            else
                _isTraceOn = false;
        }

        public static bool IsTraceOn()
        {
            return _isTraceOn;
        }

        public async Task WriteTrace(string NodeName, string Title, string Binary, EnumFromTo MsgFromTo, string RemoteAddress)
        {
            if (IsTraceOn() == false) return;

            if (_logService.IsRunning() == false)
            {
                await _logger.LogAsync("WARNING: log service is not running", "", NodeName);
                return;
            }

            //send to log services
            string header;
            if (MsgFromTo == EnumFromTo.From)
                header = $"<{Title}> Message from {NodeName} {RemoteAddress}"; //e.g. <0200> message from TM
            else
                header = $"<{Title}> Message to {NodeName} {RemoteAddress}"; //e.g. <0200> message to TM

            var req = new LogModel
            {
                LogType = LogType.Transaction,
                Datetime = DateTime.Now,
                AppName = _appname,
                FileName = NodeName,
                Title = header,
                Detail = Binary
            };

            //construct log
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(req);

            //send
            await _logService.SendAsync(json);
        }

        public async Task WriteTraceStatus(string NodeName, string Info, string Detail)
        {
            if (IsTraceOn() == false) return;

            if (_logService.IsRunning() == false)
            {
                await _logger.LogAsync("WARNING: log service is not running", "", NodeName);
                return;
            }

            //send to log services
            var req = new LogModel
            {
                LogType = LogType.Info,
                Datetime = DateTime.Now,
                AppName = _appname,
                FileName = NodeName,
                Title = Info,
                Detail = Detail
            };

            //construct log
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(req);

            //send
            await _logService.SendAsync(json);
        }

        public static async Task WriteLog(string ErrMessage)
        {
            await _logger.LogAsync(ErrMessage);
        }

        public static async Task WriteLog(string ErrMessage, string Detail)
        {
            await _logger.LogAsync(ErrMessage, Detail);
        }

        public static async Task WriteLog(string ErrMessage, string Detail, string NodeName)
        {
            await _logger.LogAsync(ErrMessage, Detail, NodeName);
        }

        public static async Task Logger(string ErrMessage)
        {
            //Console.WriteLine(ErrMessage);

            await Logger(ErrMessage, "", "");
        }

        public static async Task Logger(string ErrMessage, string Detail)
        {
            //Console.WriteLine(ErrMessage);

            await Logger(ErrMessage, Detail, "");
        }

        public static async Task Logger(string ErrMessage, string Detail, string NodeName)
        {
            await _logger.LogAsync(ErrMessage, Detail, NodeName);

            if (IsTraceOn() == false) return;
            if (_logService.IsRunning() == false) return;

            //make sure filename is not empty
            if (string.IsNullOrEmpty(NodeName) == true) NodeName = _appname;

            var req = new LogModel
            {
                LogType = LogType.Info,
                Datetime = DateTime.Now,
                AppName = _appname,
                FileName = NodeName,
                Title = ErrMessage,
                Detail = Detail
            };

            //construct log
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(req);

            //send
            await _logService.SendAsync(json);
        }


        #region Interfaces
        public static async Task ProcessMsgFromSinkNode(string NodeName, Message.Request MsgRequest)
        {
            try
            {
                //raise event
                await _iapp.ProcessMsgFromSinkNode(NodeName, MsgRequest);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex.Message);
            }
        }

        public static async Task ProcessMsgFromSourceNode(string NodeName, Message.Response MsgResponse)
        {
            try
            {
                string ConnectionName = MsgResponse.private_data.connection_name;

                //raise event
                await _iapp.ProcessMsgFromSourceNode(NodeName, ConnectionName, MsgResponse);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex.Message);
            }
        }

        public static async Task ProcessMsgFromRemoteTcp(string NodeName, string ConnectionName, byte[] Bytes, int TotalBytes, System.Net.EndPoint RemoteEP)
        {
            try
            {
                await _iapp.ProcessMsgFromRemoteTcp(NodeName, ConnectionName, Bytes, TotalBytes, RemoteEP);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex.Message);
            }
        }

        public static async Task ProcessMsgFromRemoteWsServer(string NodeName, string ConnectionName, string WSMessage, HttpContext Ctx)
        {
            try
            {
                await _iapp.ProcessMsgFromRemoteWsServer(NodeName, ConnectionName, WSMessage, Ctx);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex.Message);
            }
        }

        public static async Task ProcessMsgFromRemoteWsClient(string NodeName, string ConnectionName, Message.Request MsgOriginal, string WSMessage, HttpStatusCode StatusCode)
        {
            try
            {
                await _iapp.ProcessMsgFromRemoteWsClient(NodeName, ConnectionName, MsgOriginal, WSMessage, StatusCode);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex.Message);
            }
        }

        public static async Task OnError(string NodeName, string ConnectionName, Message.Request MsgOriginal, string ErrorMessage)
        {
            try
            {
                await _iapp.OnError(NodeName, ConnectionName, MsgOriginal, ErrorMessage);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex.Message);
            }
        }

        public static async Task TimerAutoSignOn(string NodeName)
        {
            await _iapp.TimerAutoSignon(NodeName);
        }

        public static async Task TimerEcho(string NodeName)
        {
            await _iapp.TimerEcho(NodeName);
        }

        public static async Task TimerKeyExchange(string NodeName)
        {
            await _iapp.TimerKeyExchange(NodeName);
        }

        private async Task RequestCommand(byte[] Data, EndPoint remoteEP)
        {
            try
            {
                string data = Encoding.UTF8.GetString(Data);
                string listcmd = "VERSION,RESYNC,ECHO,SIGNON,SIGNOFF,KEYCHANGE,OTHER,TRACE ON,TRACE OFF,TRACE CLEAR";
                string msg = "Receive command: " + data;
                string rsp = "OK";

                await _logger.LogAsync(msg);

                // --- Parsing command & sisa string ---
                string[] tokens = data.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string command = (tokens.Length > 0 ? tokens[0] : string.Empty);
                string rest = string.Empty;
                string nodeName = string.Empty;

                // cek dua kata pertama sebagai command
                if (tokens.Length >= 2)
                {
                    string twoWordCmd = tokens[0] + " " + tokens[1];
                    if (listcmd.Split(',').Any(cmd => cmd.Equals(twoWordCmd, StringComparison.OrdinalIgnoreCase)))
                    {
                        command = twoWordCmd;
                        int idx = data.IndexOf(twoWordCmd) + twoWordCmd.Length;
                        rest = data.Substring(idx).Trim();
                        nodeName = rest; // default nodeName = sisa
                    }
                    else
                    {
                        // default: command satu kata
                        int idx = data.IndexOf(command) + command.Length;
                        rest = data.Substring(idx).Trim();
                        nodeName = rest;
                    }
                }

                // --- Logic khusus untuk OTHER ---
                if (command.Equals("OTHER", StringComparison.OrdinalIgnoreCase))
                {
                    if (tokens.Length >= 2)
                    {
                        nodeName = tokens[1]; // NodeName = kata kedua
                                              // ambil sisa setelah NodeName
                        int idx = data.IndexOf(nodeName) + nodeName.Length;
                        rest = data.Substring(idx).Trim();
                    }
                }

                // cek apakah command ada di listcmd
                bool match = listcmd
                    .Split(',')
                    .Any(cmd => cmd.Equals(command, StringComparison.OrdinalIgnoreCase));

                // --- raise event ---
                if (match == true)
                {
                    switch (command)
                    {
                        case CMD_VERSION:
                            rsp = _version;
                            break;
                        case CMD_RESYNC:
                            await Resync();
                            await _iapp.Resync();
                            break;
                        case CMD_ECHO:
                            await _iapp.NetworkManagement(EnumNtwrkMgmt.Echo, nodeName, "");
                            break;
                        case CMD_SIGNON:
                            await _iapp.NetworkManagement(EnumNtwrkMgmt.SignOn, nodeName, "");
                            break;
                        case CMD_SIGNOFF:
                            await _iapp.NetworkManagement(EnumNtwrkMgmt.SignOff, nodeName, "");
                            break;
                        case CMD_KEYCHANGE:
                            await _iapp.NetworkManagement(EnumNtwrkMgmt.KeyChange, nodeName, "");
                            break;
                        case CMD_OTHER:
                            await _iapp.NetworkManagement(EnumNtwrkMgmt.Other, nodeName, rest);
                            break;
                        case CMD_TRACE_ON:
                            _isTraceOn = true;
                            break;
                        case CMD_TRACE_OFF:
                            _isTraceOn = false;
                            break;
                        default:
                            rsp = "Unknown command";
                            break;
                    }
                }
                else
                {
                    rsp = "Unknown command";
                }

                //reply
                await _cmd.SendToAsync(rsp, remoteEP);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex.Message);
            }
        }
        #endregion
    }
}
