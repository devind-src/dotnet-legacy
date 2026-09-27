using Newtonsoft.Json;
using SyncNet.DbRepository;
using SyncNet.Helpers;
using SyncNet.Networking;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static SyncNet.DbRepository.DbMgr;

namespace SyncNet.Nodes
{
    class SinkNode
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

        public SinkNode()
        {
            _dbMgr = new DbMgr();

            _tcpClient = new XTcpClientSdk();
            _tcpClient.OnDataArrival += OnDataArrival;
            _tcpClient.OnConnect += OnConnect;
            _tcpClient.OnDisconnect += OnDisconnect;
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
            await AppProcessor.Logger($"{this.NodeName} sink connection closed");

            //close socket
            await _tcpClient.DisconnectAsync();
        }

        public async Task Reply(Message.Response MsgResponse)
        {
            //construct message
            string WSMessage = JsonConvert.SerializeObject(MsgResponse);

            //send
            await _tcpClient.SendAsync(WSMessage);
        }

        private async Task OnError(Exception ex)
        {
            await AppProcessor.Logger($"{NodeName} sink : {ex.Message}");
        }

        private async Task OnDisconnect(EndPoint endPoint)
        {
            //update sink node
            await _dbMgr.UpdateNodeSink(NodeName, EnumStatusApp.DOWN);

            await AppProcessor.Logger($"{NodeName} sink disconnected from {NetHelper.GetRemoteEP(endPoint)}");
        }

        private async Task OnConnect(EndPoint endPoint)
        {
            //update sink node
            await _dbMgr.UpdateNodeSink(NodeName, EnumStatusApp.UP);

            await AppProcessor.Logger($"{NodeName} sink connected to {NetHelper.GetRemoteEP(endPoint)}");
        }

        private async Task OnDataArrival(byte[] Data, EndPoint remoteEP)
        {
            //get string
            string WSMessage = Encoding.UTF8.GetString(Data);

            //extract message
            Message.Request req =
                JsonConvert.DeserializeObject<Message.Request>(WSMessage);

            //pooling to transaction manager
            await AppProcessor.ProcessMsgFromSinkNode(this.NodeName, req);
        }
    }
}
