using Newtonsoft.Json;
using SyncNet.DbRepository;
using SyncNet.Helpers;
using SyncNet.Library;
using SyncNet.Networking;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static SyncNet.DbRepository.DbMgr;

namespace SyncNet.Nodes
{
    class SourceNode
    {
        //variable
        public string NodeName { get; set; }
        public string InstID { get; set; }
        public string AutoReversal { get; set; }

        public int Port { get; set; }
        public int RequestTimeout { get; set; }
        public int AdviceTimeout { get; set; }
        public int MaxRetrySend { get; set; }

        private readonly XTcpClientSdk _tcpClient;
        private readonly DbMgr _dbMgr;

        public SourceNode()
        {
            _dbMgr = new DbMgr();

            _tcpClient = new XTcpClientSdk();
            _tcpClient.OnConnect += OnConnect;
            _tcpClient.OnDisconnect += OnDisconnect;
            _tcpClient.OnDataArrival += OnDataArrival;
            _tcpClient.OnError += OnError;
        }

        public async Task Start()
        {
            try
            {
                //retry koneksi ke internal setiap 5 detik
                _tcpClient.RetryDelaySeconds = 5;

                //connect to node
                await _tcpClient.ConnectAsync("127.0.0.1", Port);
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger(ex.Message);
            }
        }

        public async Task Stop()
        {
            await AppProcessor.Logger($"{this.NodeName} source connection close");

            //release socket
            await _tcpClient.DisconnectAsync();
        }

        public async Task Send(Message.Request MsgRequest)
        {
            //construct message
            string WSMessage = JsonConvert.SerializeObject(MsgRequest);

            //get bytes
            byte[] bytes = NbConvert.StringToBytes(WSMessage);

            //send
            await _tcpClient.SendAsync(bytes);
        }

        private async Task OnError(Exception ex)
        {
            await AppProcessor.Logger($"{NodeName} source : {ex.Message}");
        }

        private async Task OnDataArrival(byte[] Data, EndPoint remoteEP)
        {
            //get string
            string WSMessage = Encoding.UTF8.GetString(Data);

            //extract message response
            Message.Response rsp =
                JsonConvert.DeserializeObject<Message.Response>(WSMessage);

            //pooling to transaction manager
            await AppProcessor.ProcessMsgFromSourceNode(NodeName, rsp);
        }

        private async Task OnDisconnect(EndPoint ep)
        {
            //update source node
            await _dbMgr.UpdateNodeSource(NodeName, EnumStatusApp.DOWN);

            await AppProcessor.Logger($"{NodeName} source disconnected from {NetHelper.GetRemoteEP(ep)}");
        }

        private async Task OnConnect(EndPoint ep)
        {
            //update source node
            await _dbMgr.UpdateNodeSource(NodeName, EnumStatusApp.UP);

            await AppProcessor.Logger($"{NodeName} source connected to {NetHelper.GetRemoteEP(ep)}");
        }
    }
}
