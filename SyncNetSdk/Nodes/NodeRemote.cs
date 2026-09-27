using Microsoft.AspNetCore.Http;
using SyncNet.Constants;
using SyncNet.DbRepository;
using SyncNet.Helpers;
using SyncNet.Library;
using SyncNet.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static SyncNet.DbRepository.DbMgr;

namespace SyncNet.Nodes
{
    public class NodeRemote : IDisposable
    {
        #region Constanta & Variable
        //constanta
        public const byte CONN_AS_SERVER = 0;
        public const byte CONN_AS_CLIENT = 1;
        public const byte TCP_HEADER_ASC = 0;
        public const byte TCP_HEADER_BCD = 1;

        //variable public - general
        public int NodeID { get; set; }
        public string NodeName { get; set; }
        public string ConnectionName { get; set; }
        public string AutoSignon { get; set; }
        public string PinTranslate { get; set; }
        public string ProtectSensitiveData { get; set; }
        public string InstID { get; set; }
        public string Parameter { get; set; }
        public byte MsgProtocol { get; set; }
        public int EchoDuration { get; set; } //in minutes
        public int KeyExchangeDuration { get; set; } //in minutes
        public int RequestTimeout { get; set; } //in seconds
        public int AdviceTimeout { get; set; } //in seconds

        //variable public - tcp-ip
        public byte ConnectionType { get; set; }         //0-Server 1-Client
        public byte TCPHeaderLength { get; set; } = 2;    //header tcp default is 2 bytes
        public byte TCPHeaderFormat { get; set; }        //tcp header format 0-ASC,1-BCD
        public int RemotePort { get; set; }
        public int LocalPort { get; set; }
        public int MaxConnection { get; set; }
        public int RetryDelay { get; set; }
        public string RemoteHost { get; set; }
        public string LocalHost { get; set; }
        public string AlwaysConnected { get; set; }
        public string TcpHighLowByte { get; set; }
        public string OneSocketOnly { get; set; }

        //variable public - web service
        public string WSUrl { get; set; }
        public string WSHeader { get; set; }
        public string WSMethod { get; set; }
        public string WSContent { get; set; }
        public string WSIPSource { get; set; }
        public string WSKey { get; set; }
        public string WSUser { get; set; }
        public string WSPswd { get; set; }
        public string WSProxyUrl { get; set; }
        public string WSProxyPort { get; set; }

        //private readonly System.Timers.Timer _TmrAutoSignon;
        private readonly DbMgr _dbMgr;

        //protocol tcp-ip
        private readonly XTcpClientSdk _tcpClient;
        private readonly XTcpListenerSdk _tcpServer;

        //protocol web service
        private XHttpClientSdk _httpClient;
        private XKestrel _httpServer;

        //timer
        private CancellationTokenSource _ctsTimer;
        private readonly List<Task> _timerTasks = [];

        private SdkTcpHeaderFormat _TcpHeaderFormat;
        private byte _HeaderLength;

        private EnumStatusApp _LastStatus;
        private bool _IsFirstRun = true;
        private bool disposedValue;
        #endregion

        public NodeRemote()
        {
            _dbMgr = new DbMgr();

            //tcp client
            _tcpClient = new XTcpClientSdk();
            _tcpClient.OnConnect += TcpClientOnConnect;
            _tcpClient.OnDataArrival += TcpClientOnDataArrival;
            _tcpClient.OnDisconnect += TcpClientOnDisconnect;
            _tcpClient.OnError += TcpClientOnError;
            _tcpClient.OnStopped += TcpClientOnStopped;

            //tcp server
            _tcpServer = new XTcpListenerSdk();
            _tcpServer.OnConnect += TcpServerOnConnect;
            _tcpServer.OnDataArrival += TcpServerOnDataArrival;
            _tcpServer.OnDisconnect += TcpServerOnDisconnect;
            _tcpServer.OnError += TcpServerOnError;
            _tcpServer.OnStopped += TcpServerOnStopped;

            ////timer
            //_TmrAutoSignon = new System.Timers.Timer();
            //_TmrAutoSignon.Elapsed += TimerAutoSignon;
            //_TmrAutoSignon.Interval = 3000;
            //_TmrAutoSignon.Enabled = false;
            //_TmrAutoSignon.Stop();
        }

        public async Task Start()
        {
            try
            {
                //update node
                await _dbMgr.UpdateNodeRemote(NodeName, EnumStatusApp.UP);

                //check protocol
                switch (MsgProtocol)
                {
                    //web service
                    case TypeProtocol.WebService:
                        if (ConnectionType == CONN_AS_SERVER)
                            await StartHttpServer();
                        else
                            await StartHttpClient();

                        break;

                    //TCP-IP protocol
                    case TypeProtocol.TCP2ByteExcludeHeader:
                    case TypeProtocol.TCP2ByteIncludeHeader:
                    case TypeProtocol.TCP4ByteExcludeHeader:
                    case TypeProtocol.TCP4ByteIncludeHeader:
                    case TypeProtocol.TCPHeaderCustom:
                    case TypeProtocol.TCPHeaderNone:
                        if (ConnectionType == CONN_AS_SERVER)
                            await StartTcpServer();
                        else
                            await StartTcpClient();
                        break;
                }

                // Jalankan timer tanpa memblokir StartAsync(), tetapi simpan referensinya
                StartTimers();
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger(ex.Message);
            }
        }
        public async Task Close()
        {
            try
            {
                //update node
                await _dbMgr.UpdateNodeRemote(NodeName, EnumStatusApp.DOWN);

                //check protocol
                switch (MsgProtocol)
                {
                    //web service
                    case TypeProtocol.WebService:
                        if (ConnectionType == CONN_AS_SERVER && _httpServer != null)
                        {
                            _httpServer.OnDataArrival -= HttpServerOnDataArrival;
                            _httpServer.OnError -= HttpServerOnError;

                            await _httpServer.StopAsync();
                            _httpServer = null;
                        }
                        else if (_httpClient != null)
                        {
                            _httpClient.OnDataArrival -= HttpClientOnDataArrival;
                            _httpClient.OnError -= HttpClientOnError;

                            await _httpClient.DisposeAsync();
                            _httpClient = null;
                        }

                        break;

                    //TCP-IP protocol
                    case TypeProtocol.TCP2ByteExcludeHeader:
                    case TypeProtocol.TCP2ByteIncludeHeader:
                    case TypeProtocol.TCP4ByteExcludeHeader:
                    case TypeProtocol.TCP4ByteIncludeHeader:
                    case TypeProtocol.TCPHeaderCustom:
                    case TypeProtocol.TCPHeaderNone:
                        //close tcp
                        try
                        {
                            if (ConnectionType == CONN_AS_SERVER)
                            {
                                await _tcpServer.StopAsync();
                            }
                            else
                            {
                                if (AlwaysConnected == "1")
                                    await _tcpClient.DisconnectAsync();
                            }
                        }
                        catch { }

                        break;
                }
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger(ex.Message);
            }
            finally
            {
                await StopTimers();
            }
        }

        #region Periodic Timer
        private void StartTimers()
        {
            _ctsTimer = new CancellationTokenSource();

            _timerTasks.Add(StartUpdateStatusNodeTimer(_ctsTimer.Token));

            if (KeyExchangeDuration > 0)
            {
                _timerTasks.Add(StartKeyExchangeTimer(_ctsTimer.Token));
            }

            if (EchoDuration > 0)
            {
                _timerTasks.Add(StartEchoTestTimer(_ctsTimer.Token));
            }
        }
        private async Task StopTimers()
        {
            _ctsTimer?.Cancel();

            try
            {
                await Task.WhenAll(_timerTasks);
            }
            catch (OperationCanceledException)
            {
                // Normal saat timer dihentikan.
            }
            finally
            {
                _ctsTimer?.Dispose();
                _ctsTimer = null;
                _timerTasks.Clear();
            }
        }
        public async Task RestartTimers()
        {
            await StopTimers();
            StartTimers();
        }

        private async Task StartUpdateStatusNodeTimer(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    try
                    {
                        await TimerUpdateNode();
                    }
                    catch (Exception ex)
                    {
                        // Log exception agar loop tidak mati jika terjadi error teknis
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal saat CancellationToken dikontak via StopAsync()
            }
        }
        private async Task StartKeyExchangeTimer(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(this.KeyExchangeDuration));
            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    try
                    {
                        await TimerKeyExchange();
                    }
                    catch (Exception ex)
                    {
                        // Log exception agar loop tidak mati jika terjadi error teknis
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal saat CancellationToken dikontak via StopAsync()
            }
        }
        private async Task StartEchoTestTimer(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(this.EchoDuration));
            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    try
                    {
                        await TimerEchoTest();
                    }
                    catch (Exception ex)
                    {
                        // Log exception agar loop tidak mati jika terjadi error teknis
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal saat CancellationToken dikontak via StopAsync()
            }
        }
        #endregion

        #region Protocol TCP/IP
        private async Task StartTcpServer()
        {
            //log startup
            await AppProcessor.Logger($"Interface {NodeName} listening on port {LocalPort}");

            //set tcp header format
            if (TCPHeaderFormat == TCP_HEADER_ASC)
                _tcpServer.HeaderFormat = SdkTcpHeaderFormat.ASC;
            else
                _tcpServer.HeaderFormat = SdkTcpHeaderFormat.BCD;

            //set header length
            switch (MsgProtocol)
            {
                case TypeProtocol.TCP2ByteExcludeHeader:
                    _tcpServer.HeaderLength = 2;
                    _tcpServer.HeaderLengthMode = SdkTcpHeaderLengthMode.ExcludeHeader;
                    break;
                case TypeProtocol.TCP2ByteIncludeHeader:
                    _tcpServer.HeaderLength = 2;
                    _tcpServer.HeaderLengthMode = SdkTcpHeaderLengthMode.IncludeHeader;
                    break;
                case TypeProtocol.TCP4ByteExcludeHeader:
                    _tcpServer.HeaderLength = 4;
                    _tcpServer.HeaderLengthMode = SdkTcpHeaderLengthMode.ExcludeHeader;
                    break;
                case TypeProtocol.TCP4ByteIncludeHeader:
                    _tcpServer.HeaderLengthMode = SdkTcpHeaderLengthMode.IncludeHeader;
                    _tcpServer.HeaderLength = 4;
                    break;
                case TypeProtocol.TCPHeaderCustom:
                    _tcpServer.HeaderLength = 2;
                    break;
                case TypeProtocol.TCPHeaderNone:
                    _tcpServer.HeaderLength = 0;
                    break;
                default:
                    _tcpServer.HeaderLength = 2;
                    _tcpServer.HeaderLengthMode = SdkTcpHeaderLengthMode.ExcludeHeader;
                    break;
            }

            //tcp header high low byte 2 bytes
            _tcpServer.HeaderHiLo = TcpHighLowByte == "0" ? false : true;

            // Persistent & non persistent connection
            _tcpServer.Persistent = AlwaysConnected == "1" ? true : false;

            _tcpServer.MaxConnection = MaxConnection;
            _tcpServer.OneSocketOnly = OneSocketOnly == "1";

            //update config protocol tcp
            _tcpServer.SetProtocol();

            //listening
            await _tcpServer.StartAsync(GetLocalIpAddress(), LocalPort);
        }
        private async Task StartTcpClient()
        {
            if (AlwaysConnected == "0") return;

            //log startup
            await AppProcessor.Logger($"Interface {NodeName} ready, remote address -> {RemoteHost}:{RemotePort}");

            //set header length
            switch (this.MsgProtocol)
            {
                case TypeProtocol.TCP2ByteExcludeHeader:
                    _HeaderLength = 2;

                    _tcpClient.HeaderLength = 2;
                    _tcpClient.HeaderLengthMode = SdkTcpHeaderLengthMode.ExcludeHeader;
                    break;
                case TypeProtocol.TCP2ByteIncludeHeader:
                    _HeaderLength = 2;

                    _tcpClient.HeaderLength = 2;
                    _tcpClient.HeaderLengthMode = SdkTcpHeaderLengthMode.IncludeHeader;
                    break;
                case TypeProtocol.TCP4ByteExcludeHeader:
                    _HeaderLength = 4;

                    _tcpClient.HeaderLength = 4;
                    _tcpClient.HeaderLengthMode = SdkTcpHeaderLengthMode.ExcludeHeader;
                    break;
                case TypeProtocol.TCP4ByteIncludeHeader:
                    _HeaderLength = 4;

                    _tcpClient.HeaderLength = 4;
                    _tcpClient.HeaderLengthMode = SdkTcpHeaderLengthMode.IncludeHeader;
                    break;
                case TypeProtocol.TCPHeaderCustom:
                    _HeaderLength = 2;

                    _tcpClient.HeaderLength = 2;
                    break;
                case TypeProtocol.TCPHeaderNone:
                    _HeaderLength = 0;

                    _tcpClient.HeaderLength = 0;
                    break;
                default:
                    _HeaderLength = 2;

                    _tcpClient.HeaderLength = 2;
                    _tcpClient.HeaderLengthMode = SdkTcpHeaderLengthMode.ExcludeHeader;
                    break;
            }

            //tcp header high low byte 2 bytes
            if (this.TcpHighLowByte == "0")
                _tcpClient.HeaderHiLo = false;
            else
                _tcpClient.HeaderHiLo = true;

            //set tcp header format
            if (this.TCPHeaderFormat == TCP_HEADER_ASC)
            {
                _TcpHeaderFormat = SdkTcpHeaderFormat.ASC;
                _tcpClient.HeaderFormat = SdkTcpHeaderFormat.ASC;
            }
            else
            {
                _TcpHeaderFormat = SdkTcpHeaderFormat.BCD;
                _tcpClient.HeaderFormat = SdkTcpHeaderFormat.BCD;
            }

            //update config protocol tcp
            _tcpClient.RetryDelaySeconds = RetryDelay;
            _tcpClient.SetProtocol();

            //connect to server
            await _tcpClient.ConnectAsync(RemoteHost, RemotePort);
        }

        private async Task TcpServerOnStopped(string message)
        {
            await AppProcessor.Logger(message, "", NodeName);
        }
        private async Task TcpServerOnError(Exception ex, EndPoint ep)
        {
            await AppProcessor.Logger(ex.Message, "", NodeName);
        }
        private async Task TcpServerOnDisconnect(EndPoint ep)
        {
            string msg = $"Interchange {NodeName} ({ConnectionName}) disconnected at address {NetHelper.GetRemoteEP(ep)}";
            await AppProcessor.Logger(msg, "", NodeName);

            if (AlwaysConnected == "1")
            {
                //update connection
                await _dbMgr.UpdateConnection(ConnectionName, EnumStatusApp.DOWN);
            }
        }
        private async Task TcpServerOnDataArrival(byte[] Data, EndPoint remoteEP)
        {
            try
            {
                await AppProcessor.ProcessMsgFromRemoteTcp(this.NodeName, this.ConnectionName,
                    Data, Data.Length, remoteEP);
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger(NodeName, ex.Message, "");
            }
        }
        private async Task TcpServerOnConnect(EndPoint ep)
        {
            string msg = $"Interchange {NodeName} ({ConnectionName}) connected at address {NetHelper.GetRemoteEP(ep)}";
            await AppProcessor.Logger(msg, "", NodeName);

            if (AlwaysConnected == "1")
            {
                //update connection
                await _dbMgr.UpdateConnection(ConnectionName, EnumStatusApp.UP);

                if (this.AutoSignon == "1")
                {
                    await TimerAutoSignon();
                }
            }
        }

        private async Task TcpClientOnStopped(string message)
        {
            await AppProcessor.Logger(message, "", NodeName);
        }
        private async Task TcpClientOnError(Exception ex)
        {
            await AppProcessor.Logger(ex.Message, "", NodeName);
        }
        private async Task TcpClientOnDisconnect(EndPoint ep)
        {
            //update connection
            await _dbMgr.UpdateConnection(ConnectionName, EnumStatusApp.DOWN);

            string msg = $"Interchange {NodeName} ({ConnectionName}) disconnected at address {NetHelper.GetRemoteEP(ep)}";
            await AppProcessor.Logger(msg, "", NodeName);
        }
        private async Task TcpClientOnDataArrival(byte[] Data, EndPoint remoteEP)
        {
            try
            {
                await AppProcessor.ProcessMsgFromRemoteTcp(this.NodeName, this.ConnectionName,
                    Data, Data.Length, remoteEP);
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger(NodeName, ex.Message, "");
            }
        }
        private async Task TcpClientOnConnect(EndPoint ep)
        {
            //update connection
            await _dbMgr.UpdateConnection(ConnectionName, EnumStatusApp.UP);

            string msg = $"Interchange {NodeName} ({ConnectionName}) connected at address {NetHelper.GetRemoteEP(ep)}";
            await AppProcessor.Logger(msg, "", NodeName);

            if (this.AutoSignon == "1")
            {
                await TimerAutoSignon();
            }
        }

        public string GetEndPointClient()
        {
            string ret = string.Empty;

            if (ConnectionType == CONN_AS_SERVER)
            {
                if (AlwaysConnected == "1")
                {
                    ret = _tcpServer.GetEndPointClient() is IPEndPoint ep
                        ? $"{ep.Address}:{ep.Port}" : string.Empty;
                }
            }
            else
            {
                ret = $"{this.RemoteHost}:{this.RemotePort}";
            }

            return ret;
        }
        public List<string> GetListEndPointClient()
        {
            var ret = new List<string>();

            if (AlwaysConnected == "1")
                ret = _tcpServer.GetListEndPointClient() is IEnumerable<IPEndPoint> eps
                    ? eps.Select(ep => $"{ep.Address}:{ep.Port}").ToList()
                    : [];

            return ret;
        }

        public bool IsConnected()
        {
            bool result = false;

            switch (MsgProtocol)
            {
                //TCP-IP protocol
                case TypeProtocol.TCP2ByteExcludeHeader:
                case TypeProtocol.TCP2ByteIncludeHeader:
                case TypeProtocol.TCP4ByteExcludeHeader:
                case TypeProtocol.TCP4ByteIncludeHeader:
                case TypeProtocol.TCPHeaderCustom:
                case TypeProtocol.TCPHeaderNone:
                    if (ConnectionType == CONN_AS_SERVER)
                    {
                        if (AlwaysConnected == "1")
                            result = _tcpServer.IsConnected();
                    }
                    else
                    {
                        result = _tcpClient.IsConnected();
                    }

                    break;
                default:
                    result = true;
                    break;
            }

            return result;
        }
        public bool IsProtocolTCP()
        {
            bool bval = false;

            switch (this.MsgProtocol)
            {
                case TypeProtocol.TCP2ByteExcludeHeader:
                case TypeProtocol.TCP2ByteIncludeHeader:
                case TypeProtocol.TCP4ByteExcludeHeader:
                case TypeProtocol.TCP4ByteIncludeHeader:
                case TypeProtocol.TCPHeaderCustom:
                case TypeProtocol.TCPHeaderNone:
                    bval = true;
                    break;
            }

            return bval;
        }

        public async Task ResetTcp()
        {
            if (ConnectionType == CONN_AS_SERVER)
            {
                //close socket
                await _tcpServer.StopAsync();

                //delay 1 sec
                await Task.Delay(1000);

                //re-listening
                await _tcpServer.StartAsync(GetLocalIpAddress(), LocalPort);
            }
            else
            {
                if (AlwaysConnected == "1")
                {
                    //close socket
                    await _tcpClient.DisconnectAsync();

                    //delay 1 sec
                    await Task.Delay(1000);

                    //reconnect to server
                    await _tcpClient.ConnectAsync(RemoteHost, RemotePort);
                }
            }
        }
        #endregion 

        #region Protocol HTTP
        private async Task StartHttpServer()
        {
            //log startup
            await AppProcessor.Logger($"Interface {NodeName} listening on {this.WSUrl}");

            _httpServer = new XKestrel();
            _httpServer.OnDataArrival += HttpServerOnDataArrival;
            _httpServer.OnError += HttpServerOnError;

            //set parameter and start
            _httpServer.Method = this.WSMethod;
            _httpServer.MaxWait = RequestTimeout;

            await _httpServer.StartAsync(this.WSUrl);
        }
        private async Task HttpServerOnError(Exception ex)
        {
            await AppProcessor.Logger(ex.Message);
        }
        private async Task HttpServerOnDataArrival(string MsgRequest, HttpContext Ctx)
        {
            try
            {
                await AppProcessor.ProcessMsgFromRemoteWsServer(this.NodeName, this.ConnectionName,
                    MsgRequest, Ctx);
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger(NodeName, ex.Message, "");
            }
        }

        private async Task StartHttpClient()
        {
            //log startup
            await AppProcessor.Logger($"Interface {NodeName} ready, url client -> {this.WSUrl}");

            _httpClient = new XHttpClientSdk(this.WSUrl, this.WSProxyUrl,
                NbConvert.ToInt(this.WSProxyPort), this.RequestTimeout);

            _httpClient.OnDataArrival += HttpClientOnDataArrival;
            _httpClient.OnError += HttpClientOnError;

            //set parameter and start
            _httpClient.WsMethod = this.WSMethod;
            _httpClient.WsContent = this.WSContent;
        }
        private async Task HttpClientOnError(Message.Request MsgOriginal, Exception ex)
        {
            await AppProcessor.OnError(this.NodeName, this.ConnectionName, MsgOriginal, ex.Message);
        }
        private async Task HttpClientOnDataArrival(Message.Request MsgOriginal, string MsgResponse, HttpStatusCode StatusCode)
        {
            try
            {
                await AppProcessor.ProcessMsgFromRemoteWsClient(this.NodeName, this.ConnectionName,
                    MsgOriginal, MsgResponse, StatusCode);
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger(NodeName, ex.Message, "");
            }
        }
        #endregion Web Service

        #region Timers
        private async Task TimerAutoSignon()
        {
            //delay 3 seconds before send signon
            await Task.Delay(TimeSpan.FromSeconds(3));

            //raise event
            await AppProcessor.TimerAutoSignOn(this.NodeName);
        }
        private async Task TimerUpdateNode()
        {
            if (ConnectionType == CONN_AS_CLIENT && AlwaysConnected == "0")
                return;

            EnumStatusApp status;

            if (MsgProtocol == TypeProtocol.WebService ||
                MsgProtocol == TypeProtocol.MessageQueue ||
                MsgProtocol == TypeProtocol.Custom)
            {
                // HTTP tidak memiliki status socket TCP yang dapat diperiksa di sini.
                status = EnumStatusApp.UP;
            }
            else if (ConnectionType == CONN_AS_SERVER)
                status = _tcpServer.IsConnected() == true
                    ? EnumStatusApp.UP
                    : EnumStatusApp.DOWN;
            else
                status = _tcpClient.IsConnected() == true
                    ? EnumStatusApp.UP
                    : EnumStatusApp.DOWN;

            //*** check if only change ***//
            if (_IsFirstRun == true)
            {
                _IsFirstRun = false;
                _LastStatus = status;
            }
            else if (_LastStatus != status)
            {
                _LastStatus = status;
            }

            await _dbMgr.UpdateConnection(ConnectionName, status);
        }
        private async Task TimerKeyExchange()
        {
            if (this.KeyExchangeDuration > 0)
                await AppProcessor.TimerKeyExchange(this.NodeName);
        }
        private async Task TimerEchoTest()
        {
            if (this.EchoDuration > 0)
                await AppProcessor.TimerEcho(this.NodeName);
        }
        #endregion

        public async Task SendToTcp(byte[] bytes)
        {
            await ReplyTcp(bytes, null);
        }
        public async Task SendToHttpClient(Message.Request MsgOriginal, string MsgRequest, WebHeaderCollection Header)
        {
            if (ConnectionType == CONN_AS_CLIENT)
            {
                //set parameter and start
                _httpClient.WsUrl = this.WSUrl;
                _httpClient.WsMethod = this.WSMethod;
                _httpClient.WsContent = this.WSContent;
                _httpClient.WsTimeout = this.RequestTimeout;

                //set proxy
                _httpClient.WsProxyUrl = this.WSProxyUrl;
                _httpClient.WsProxyPort = NbConvert.ToInt(this.WSProxyPort);

                await _httpClient.Send(MsgOriginal, MsgRequest, Header);
            }
        }
        public async Task SendToHttpClient(Message.Request MsgOriginal, string MsgRequest, string Parameter, WebHeaderCollection Header)
        {
            if (ConnectionType == CONN_AS_CLIENT)
            {
                //set parameter and start
                _httpClient.WsUrl = this.WSUrl;
                _httpClient.WsMethod = this.WSMethod;
                _httpClient.WsContent = this.WSContent;
                _httpClient.WsTimeout = this.RequestTimeout;

                //set proxy
                _httpClient.WsProxyUrl = this.WSProxyUrl;
                _httpClient.WsProxyPort = NbConvert.ToInt(this.WSProxyPort);

                await _httpClient.Send(MsgOriginal, MsgRequest, Parameter, Header);
            }
        }

        public async Task ReplyTcp(byte[] bytes, EndPoint remoteEP)
        {
            try
            {
                if (ConnectionType == CONN_AS_SERVER)
                {
                    if (remoteEP == null)
                        await _tcpServer.SendAsync(bytes);
                    else
                        await _tcpServer.SendToAsync(bytes, remoteEP);
                }
                else
                {
                    if (AlwaysConnected == "1")
                    {
                        await _tcpClient.SendAsync(bytes);
                    }
                    else
                    {
                        var client = new NodeClient(this.NodeName, this.ConnectionName, this.RequestTimeout);
                        await client.Send(RemoteHost, RemotePort, bytes, _HeaderLength, _TcpHeaderFormat);
                    }
                }
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger(ex.Message);
            }
        }
        public async Task ReplyHttpServer(string WSMessage, HttpContext Ctx)
        {
            //set parameter 
            _httpServer.Method = this.WSMethod;

            await _httpServer.Reply(WSMessage, Ctx);
        }
        public async Task ReplyHttpserver(HttpStatusCode Code, HttpContext Ctx)
        {
            //set parameter 
            _httpServer.Method = this.WSMethod;

            await _httpServer.Reply("", Ctx, Code);
        }

        private IPAddress GetLocalIpAddress()
        {
            if (string.IsNullOrWhiteSpace(LocalHost) ||
                LocalHost == "0.0.0.0" ||
                LocalHost == "*")
            {
                return IPAddress.Any;
            }

            if (!IPAddress.TryParse(LocalHost, out IPAddress address))
            {
                throw new InvalidOperationException(
                    $"LocalHost tidak valid untuk node {NodeName}: {LocalHost}");
            }

            return address;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                //_TmrAutoSignon.Dispose();
                //_TmrEchoTest.Dispose();
                //_TmrKeyExchange.Dispose();
                //_TmrUpdateStatusNode.Dispose();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
