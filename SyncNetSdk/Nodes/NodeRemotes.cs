using Microsoft.AspNetCore.Http;
using SyncNet.Constants;
using SyncNet.DbRepository;
using SyncNet.Library;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SyncNet.Nodes
{
    class NodeRemotes
    {
        private readonly string _appname;

        private readonly Dictionary<string, string> _nodename = [];
        private readonly Dictionary<string, NodeRemote> _nodes = [];

        private readonly NbLogger _logger;
        private readonly DbMgr _dbMgr;

        public NodeRemotes(string AppName)
        {
            _appname = AppName;

            _logger = new NbLogger(AppProcessor.APPNAME);
            _dbMgr = new DbMgr();
        }

        public async Task Start()
        {
            await GetNewInterchange();
        }

        public async Task Stop()
        {
            foreach (var node in _nodes)
            {
                await node.Value.Close();
            }
        }

        public bool GetNode(string NodeName, out NodeRemote node)
        {
            bool bval = false;

            node = new NodeRemote();
            if (_nodename.TryGetValue(NodeName, out string key) == true)
            {
                node = _nodes[key];
                bval = true;
            }

            return bval;
        }

        public bool GetNode(string NodeName, string ConnName, out NodeRemote node)
        {
            string key = NodeName + ConnName;
            bool bval = _nodes.ContainsKey(key);

            node = bval == true ? _nodes[key] : null;

            return bval;
        }

        public NodeRemote GetNodeRemote(string NodeName)
        {
            NodeRemote node = null;

            if (_nodename.TryGetValue(NodeName, out string key) == true)
            {
                node = new NodeRemote();
                node = _nodes[key];
            }

            return node;
        }

        public NodeRemote GetNodeRemote(string NodeName, string ConnName)
        {
            NodeRemote node = null;

            string key = NodeName + ConnName;
            if (_nodes.ContainsKey(key) == true)
            {
                node = _nodes[key];
            }

            return node;
        }

        public bool IsConnected(string NodeName)
        {
            bool bval = false;

            if (_nodename.TryGetValue(NodeName, out string key) == true)
            {
                bval = _nodes[key].IsConnected();
            }

            return bval;
        }

        public bool IsConnected(string NodeName, string ConnName)
        {
            bool bval = false;

            string key = NodeName + ConnName;
            if (_nodes.ContainsKey(key) == true)
            {
                bval = _nodes[key].IsConnected();
            }

            return bval;
        }

        public async Task Resync()
        {
            await CheckNewInterchange();
            await CheckInterchangeDelete();
            await CheckInterchangeModify();
        }

        public async Task ResetTcp(string NodeName)
        {
            if (_nodename.TryGetValue(NodeName, out string key) == true)
                await _nodes[key].ResetTcp();
            else
                await _logger.LogAsync($"Node {NodeName} not found");
        }

        public async Task TcpSend(string NodeName, byte[] Bytes)
        {
            if (_nodename.TryGetValue(NodeName, out string key) == true)
                await _nodes[key].SendToTcp(Bytes);
            else
                await _logger.LogAsync($"Node {NodeName} not found");
        }

        public async Task TcpSend(string NodeName, string ConnName, byte[] Bytes)
        {
            string key = NodeName + ConnName;
            if (_nodes.ContainsKey(key) == true)
                await _nodes[key].SendToTcp(Bytes);
            else
                await _logger.LogAsync($"Node {NodeName} not found");
        }

        public async Task TcpReply(string NodeName, string ConnName, byte[] Bytes, EndPoint EP)
        {
            string key = NodeName + ConnName;
            if (_nodes.ContainsKey(key) == true)
                await _nodes[key].ReplyTcp(Bytes, EP);
            else
                await AppProcessor.Logger($"Node {NodeName} not found");
        }

        public async Task HttpClientSend(string NodeName, Message.Request MsgOriginal, string MsgRequest, WebHeaderCollection Header)
        {
            if (_nodename.TryGetValue(NodeName, out string key) == true)
                await _nodes[key].SendToHttpClient(MsgOriginal, MsgRequest, Header);
            else
                await _logger.LogAsync($"Node {NodeName} not found");
        }

        public async Task HttpClientSend(string NodeName, Message.Request MsgOriginal, string MsgRequest, string Parameter, WebHeaderCollection Header)
        {
            if (_nodename.TryGetValue(NodeName, out string key) == true)
                await _nodes[key].SendToHttpClient(MsgOriginal, MsgRequest, Parameter, Header);
            else
                await _logger.LogAsync($"Node {NodeName} not found");
        }

        public async Task HttpServerReply(string NodeName, string ConnName, string MsgResponse, HttpContext Ctx)
        {
            string key = NodeName + ConnName;
            if (_nodes.ContainsKey(key) == true)
                await _nodes[key].ReplyHttpServer(MsgResponse, Ctx);
            else
                await _logger.LogAsync($"Node {NodeName} not found");
        }

        public async Task HttpServerReply(string NodeName, string ConnName, HttpStatusCode Code, HttpContext Ctx)
        {
            string key = NodeName + ConnName;
            if (_nodes.ContainsKey(key) == true)
                await _nodes[key].ReplyHttpserver(Code, Ctx);
            else
                await _logger.LogAsync($"Node {NodeName} not found");
        }

        private async Task GetNewInterchange()
        {
            try
            {
                DataTable tbl = await _dbMgr.GetConnection(_appname);

                if (tbl.Rows.Count == 0)
                    await AppProcessor.Logger("Interface is empty, please add interface from configuration console");

                _nodes.Clear();
                _nodename.Clear();

                foreach (DataRow rec in tbl.Rows)
                {
                    var itc = new NodeRemote
                    {
                        NodeID = NbConvert.ToInt(rec["node_id"].ToString()),
                        NodeName = rec["node_name"].ToString(),
                        ConnectionName = rec["conn_name"].ToString(),
                        ConnectionType = (byte)NbConvert.ToInt(rec["conn_type"].ToString()),
                        MsgProtocol = (byte)NbConvert.ToInt(rec["protocol"].ToString()),
                        TCPHeaderFormat = (byte)NbConvert.ToInt(rec["tcp_header_format"].ToString()),
                        AutoSignon = rec["auto_signon"].ToString(),
                        EchoDuration = NbConvert.ToInt(rec["echo_timer"].ToString()),
                        KeyExchangeDuration = NbConvert.ToInt(rec["keychange_timer"].ToString()),
                        InstID = rec["inst_id"].ToString(),
                        Parameter = rec["parameter"].ToString(),
                        PinTranslate = rec["pin_translate"].ToString(),
                        ProtectSensitiveData = rec["sensitive_data"].ToString(),
                        LocalHost = rec["ip_address"].ToString(),
                        LocalPort = NbConvert.ToInt(rec["port"].ToString()),
                        MaxConnection = NbConvert.ToInt(rec["max_conn"].ToString()),
                        RetryDelay = NbConvert.ToInt(rec["retry_delay"].ToString()),
                        AlwaysConnected = rec["always_connected"].ToString(),
                        OneSocketOnly = rec["one_socket_only"].ToString(),
                        TcpHighLowByte = rec["tcp_hi_lo"].ToString(),
                        RemoteHost = rec["ip_address"].ToString(),
                        RemotePort = NbConvert.ToInt(rec["port"].ToString()),
                        RequestTimeout = NbConvert.ToInt(rec["request_timeout"].ToString()),
                        AdviceTimeout = NbConvert.ToInt(rec["advice_timeout"].ToString()),
                        WSUrl = rec["ws_url"].ToString(),
                        WSHeader = rec["ws_header"].ToString(),
                        WSMethod = rec["ws_method"].ToString(),
                        WSContent = rec["ws_content"].ToString(),
                        WSIPSource = rec["ws_ipsource"].ToString(),
                        WSKey = rec["ws_key"].ToString(),
                        WSUser = rec["ws_user"].ToString(),
                        WSPswd = rec["ws_pswd"].ToString(),
                        WSProxyUrl = rec["ws_proxy_url"].ToString(),
                        WSProxyPort = rec["ws_proxy_port"].ToString()
                    };

                    if (itc.MsgProtocol == TypeProtocol.TCPHeaderNone) //TCP Header Length -> None
                        itc.TCPHeaderLength = 0;

                    await itc.Start();

                    string key = itc.NodeName + itc.ConnectionName;

                    //add node
                    _nodes.TryAdd(key, itc);

                    //add nodename
                    if (_nodename.ContainsKey(itc.NodeName) == false)
                        _nodename.TryAdd(itc.NodeName, key);
                }
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger(ex.Message);
            }
        }

        private async Task CheckNewInterchange()
        {
            try
            {
                DataTable tbl = await _dbMgr.GetConnection(_appname);

                foreach (DataRow rec in tbl.Rows)
                {
                    string key = rec["node_name"].ToString() + rec["conn_name"].ToString();

                    if (_nodes.ContainsKey(key) == false)
                    {
                        var itc = new NodeRemote
                        {
                            NodeID = NbConvert.ToInt(rec["node_id"].ToString()),
                            NodeName = rec["node_name"].ToString(),
                            ConnectionName = rec["conn_name"].ToString(),
                            ConnectionType = (byte)NbConvert.ToInt(rec["conn_type"].ToString()),
                            MsgProtocol = (byte)NbConvert.ToInt(rec["protocol"].ToString()),
                            TCPHeaderFormat = (byte)NbConvert.ToInt(rec["tcp_header_format"].ToString()),
                            AutoSignon = rec["auto_signon"].ToString(),
                            EchoDuration = NbConvert.ToInt(rec["echo_timer"].ToString()),
                            KeyExchangeDuration = NbConvert.ToInt(rec["keychange_timer"].ToString()),
                            InstID = rec["inst_id"].ToString(),
                            Parameter = rec["parameter"].ToString(),
                            PinTranslate = rec["pin_translate"].ToString(),
                            ProtectSensitiveData = rec["sensitive_data"].ToString(),
                            LocalHost = rec["ip_address"].ToString(),
                            LocalPort = NbConvert.ToInt(rec["port"].ToString()),
                            MaxConnection = NbConvert.ToInt(rec["max_conn"].ToString()),
                            RetryDelay = NbConvert.ToInt(rec["retry_delay"].ToString()),
                            AlwaysConnected = rec["always_connected"].ToString(),
                            OneSocketOnly = rec["one_socket_only"].ToString(),
                            TcpHighLowByte = rec["tcp_hi_lo"].ToString(),
                            RemoteHost = rec["ip_address"].ToString(),
                            RemotePort = NbConvert.ToInt(rec["port"].ToString()),
                            RequestTimeout = NbConvert.ToInt(rec["request_timeout"].ToString()),
                            AdviceTimeout = NbConvert.ToInt(rec["advice_timeout"].ToString()),
                            WSUrl = rec["ws_url"].ToString(),
                            WSHeader = rec["ws_header"].ToString(),
                            WSMethod = rec["ws_method"].ToString(),
                            WSContent = rec["ws_content"].ToString(),
                            WSIPSource = rec["ws_ipsource"].ToString(),
                            WSKey = rec["ws_key"].ToString(),
                            WSUser = rec["ws_user"].ToString(),
                            WSPswd = rec["ws_pswd"].ToString(),
                            WSProxyUrl = rec["ws_proxy_url"].ToString(),
                            WSProxyPort = rec["ws_proxy_port"].ToString()
                        };

                        if (itc.MsgProtocol == TypeProtocol.TCPHeaderNone) //TCP Header Length -> None
                            itc.TCPHeaderLength = 0;

                        await itc.Start();

                        //add node
                        _nodes.TryAdd(key, itc);

                        //add nodename
                        if (_nodename.ContainsKey(itc.NodeName) == false)
                            _nodename.TryAdd(itc.NodeName, key);
                    }
                }
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger(ex.Message);
            }
        }

        private async Task CheckInterchangeDelete()
        {
            try
            {
                DataTable tbl = await _dbMgr.GetConnection(_appname);

                var tmp = new Dictionary<string, string>();
                foreach (DataRow rec in tbl.Rows)
                {
                    string nodeName = rec["node_name"].ToString();
                    string connName = rec["conn_name"].ToString();
                    string key = nodeName + connName;

                    tmp.Add(key, nodeName);
                }

                foreach (string key in _nodes.Keys.ToList())
                {
                    if (tmp.ContainsKey(key)) continue;

                    NodeRemote removedNode = _nodes[key];
                    string nodeName = removedNode.NodeName;

                    await removedNode.Close();
                    _nodes.Remove(key);

                    // Hapus mapping nama bila tidak ada koneksi lain untuk node tersebut.
                    if (!_nodes.Values.Any(node => node.NodeName == nodeName))
                    {
                        _nodename.Remove(nodeName);
                    }
                }
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger(ex.Message);
            }
        }

        private async Task CheckInterchangeModify()
        {
            try
            {
                DataTable tbl = await _dbMgr.GetConnection(_appname);

                foreach (DataRow rec in tbl.Rows)
                {
                    string nodeName = rec["node_name"].ToString();
                    string connName = rec["conn_name"].ToString();
                    string key = nodeName + connName;

                    // Pastikan node ada di dictionary sebelum diproses
                    if (!_nodes.TryGetValue(key, out NodeRemote current)) continue;

                    // Extract nilai yang sering digunakan untuk menghindari konversi berulang
                    byte connType = (byte)NbConvert.ToInt(rec["conn_type"].ToString());
                    byte protocol = (byte)NbConvert.ToInt(rec["protocol"].ToString());
                    byte tcpHeaderFormat = (byte)NbConvert.ToInt(rec["tcp_header_format"].ToString());

                    string instID = rec["inst_id"].ToString();
                    string ipAddress = rec["ip_address"].ToString();
                    string alwaysConnected = rec["always_connected"].ToString();
                    string oneSocketOnly = rec["one_socket_only"].ToString();
                    string tcpHighLowByte = rec["tcp_hi_lo"].ToString();
                    string autoSignon = rec["auto_signon"].ToString();
                    string parameter = rec["parameter"].ToString();
                    string sensitiveData = rec["sensitive_data"].ToString();
                    string pinTranslate = rec["pin_translate"].ToString();

                    string wsUrl = rec["ws_url"].ToString();
                    string wsMethod = rec["ws_method"].ToString();
                    string wsProxyUrl = rec["ws_proxy_url"].ToString();
                    string wsProxyPort = rec["ws_proxy_port"].ToString();
                    string wsHeader = rec["ws_header"].ToString();
                    string wsContent = rec["ws_content"].ToString();
                    string wsIPSource = rec["ws_ipsource"].ToString();
                    string wsKey = rec["ws_key"].ToString();
                    string wsUser = rec["ws_user"].ToString();
                    string wsPswd = rec["ws_pswd"].ToString();

                    int nodeId = NbConvert.ToInt(rec["node_id"].ToString());
                    int echoTimer = NbConvert.ToInt(rec["echo_timer"].ToString());
                    int keychangeTimer = NbConvert.ToInt(rec["keychange_timer"].ToString());

                    int port = NbConvert.ToInt(rec["port"].ToString());
                    int maxConn = NbConvert.ToInt(rec["max_conn"].ToString());
                    int requestTimeout = NbConvert.ToInt(rec["request_timeout"].ToString());
                    int adviceTimeout = NbConvert.ToInt(rec["advice_timeout"].ToString());
                    int retryDelay = NbConvert.ToInt(rec["retry_delay"].ToString());

                    // Protocol koneksi
                    bool isTcp = protocol <= TypeProtocol.TCPHeaderNone;

                    bool isHttpServer =
                        protocol == TypeProtocol.WebService &&
                        connType == NodeRemote.CONN_AS_SERVER;

                    bool isHttpClient =
                        protocol == TypeProtocol.WebService &&
                        connType == NodeRemote.CONN_AS_CLIENT;

                    bool restartTimers = current.EchoDuration != echoTimer ||
                        current.KeyExchangeDuration != keychangeTimer;

                    // Logika penentuan restart
                    bool bRestart = current.MsgProtocol != protocol || current.ConnectionType != connType ||

                         (isTcp &&
                             (current.TCPHeaderFormat != tcpHeaderFormat ||
                              current.AlwaysConnected != alwaysConnected ||
                              current.TcpHighLowByte != tcpHighLowByte ||
                              current.RetryDelay != retryDelay)) ||

                        (isTcp && connType == NodeRemote.CONN_AS_SERVER &&
                            (current.LocalHost != ipAddress ||
                             current.LocalPort != port ||
                             current.MaxConnection != maxConn ||
                             current.OneSocketOnly != oneSocketOnly)) ||

                        (isTcp && connType == NodeRemote.CONN_AS_CLIENT &&
                            (current.RemoteHost != ipAddress ||
                             current.RemotePort != port)) ||

                         (isHttpServer &&
                             (current.WSUrl != wsUrl ||
                              current.WSMethod != wsMethod ||
                              current.RequestTimeout != requestTimeout)) ||

                         (isHttpClient &&
                             (current.WSProxyUrl != wsProxyUrl ||
                              current.WSProxyPort != wsProxyPort ||
                              current.RequestTimeout != requestTimeout));

                    if (bRestart)
                    {
                        await current.Close();
                    }

                    // Pembaruan properti node (menggunakan referensi 'current' secara langsung)
                    current.NodeID = nodeId;
                    current.NodeName = nodeName;
                    current.ConnectionName = connName;
                    current.ConnectionType = connType;
                    current.MsgProtocol = protocol;
                    current.TCPHeaderFormat = tcpHeaderFormat;
                    current.AutoSignon = autoSignon;
                    current.EchoDuration = echoTimer;
                    current.KeyExchangeDuration = keychangeTimer;
                    current.InstID = instID;
                    current.Parameter = parameter;
                    current.PinTranslate = pinTranslate;
                    current.ProtectSensitiveData = sensitiveData;
                    current.LocalHost = ipAddress;
                    current.LocalPort = port;
                    current.MaxConnection = maxConn;
                    current.RetryDelay = retryDelay;
                    current.AlwaysConnected = alwaysConnected;
                    current.OneSocketOnly = oneSocketOnly;
                    current.TcpHighLowByte = tcpHighLowByte;
                    current.RemoteHost = ipAddress;
                    current.RemotePort = port;
                    current.RequestTimeout = requestTimeout;
                    current.AdviceTimeout = adviceTimeout;
                    current.WSUrl = wsUrl;
                    current.WSHeader = wsHeader;
                    current.WSMethod = wsMethod;
                    current.WSContent = wsContent;
                    current.WSIPSource = wsIPSource;
                    current.WSKey = wsKey;
                    current.WSUser = wsUser;
                    current.WSPswd = wsPswd;
                    current.WSProxyUrl = wsProxyUrl;
                    current.WSProxyPort = wsProxyPort;

                    if (current.MsgProtocol == TypeProtocol.TCPHeaderNone) //TCP Header Length -> None
                        current.TCPHeaderLength = 0;

                    if (bRestart == true)
                    {
                        await current.Start();
                    }
                    else if (restartTimers)
                    {
                        await current.RestartTimers(); // koneksi tetap aktif
                    }
                }
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger(ex.Message);
            }
        }
    }
}
