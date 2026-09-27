using SyncNet.Networking;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SyncNet.Nodes
{
    class NodeClient : IDisposable
    {
        private bool _flagSend;

        private readonly System.Timers.Timer _tmr;
        private readonly XTcpClientSdk _tcpClient;

        private readonly string _nodename;
        private readonly string _connname;

        private byte[] _bytes;

        public NodeClient(string NodeName, string ConnName, int ReqTimeout)
        {
            _flagSend = false;

            _nodename = NodeName;
            _connname = ConnName;

            int interval = (ReqTimeout - 1) * 1000;

            //used for release socket
            _tmr = new System.Timers.Timer(interval);
            _tmr.Elapsed += TimerElapsed;
            _tmr.Enabled = false;

            _tcpClient = new XTcpClientSdk();
            _tcpClient.OnDataArrival += ClientOnDataArrival;
            _tcpClient.OnConnect += ClientOnConnect;
        }

        private async void TimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                //disconnect & release object
                await _tcpClient.DisconnectAsync();

                //stop the timer & release object
                _tmr.Enabled = false;
                _tmr.Dispose();
            }
            catch { }
        }

        private async Task ClientOnConnect(EndPoint remoteEP)
        {
            if (_flagSend == false)
            {
                //make sure only send once
                _flagSend = true;

                //send
                await _tcpClient.SendAsync(_bytes);
            }
        }

        private async Task ClientOnDataArrival(byte[] Data, EndPoint remoteEP)
        {
            await AppProcessor.ProcessMsgFromRemoteTcp(_nodename, _connname, Data, Data.Length, remoteEP);
        }

        public async Task Send(string RemoteHost, int RemotePort, byte[] bytes, byte HeaderLength, SdkTcpHeaderFormat TcpHeaderFormat)
        {
            _bytes = bytes;

            _tcpClient.HeaderLength = HeaderLength;
            _tcpClient.HeaderFormat = TcpHeaderFormat;
            _tcpClient.AutoReconnect = false;

            //start the timer
            _tmr.Enabled = true;

            //connect to server
            await _tcpClient.ConnectAsync(RemoteHost, RemotePort);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                _tmr.Dispose();

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Client() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
