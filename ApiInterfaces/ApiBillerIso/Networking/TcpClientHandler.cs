using SyncNet.Helpers;
using SyncNet.Library;
using SyncNet.Networking;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ApiBiller.Networking
{
    public class TcpClientHandler
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _networkStream;

        private readonly string _server;
        private readonly int _port;

        private readonly DateTime _datetime;
        private readonly int _MaxExpired = 5;//in minutes

        public TcpClientHandler(string server, int port)
        {
            _datetime = DateTime.Now;

            _server = server;
            _port = port;

            _client = new TcpClient(server, port);
            _networkStream = _client.GetStream();
        }

        public bool IsExpired()
        {
            bool ret = false;
            TimeSpan d = DateTime.Now.Subtract(_datetime);

            if (d.TotalMinutes >= _MaxExpired)
                ret = true;

            return ret;
        }
        public bool IsConnected()
        {
            return _client.Connected;
        }

        public async Task SendDataAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (_client.Connected)
            {
                // add tcp header
                byte[] payload = TcpHelper.AddTcpHeader(data,
                    SdkTcpHeader.Tcp2ByteAscExcludeHeader);

                // gunakan ReadOnlyMemory<byte> + CancellationToken
                await _networkStream.WriteAsync(payload.AsMemory(0, payload.Length), cancellationToken);
            }
        }

        public async Task<byte[]> ReceiveDataAsync(CancellationToken cancellationToken = default)
        {
            if (!_client.Connected) return [];

            // --- baca header ukuran (2 byte) secara exact ---
            byte[] sizeBuffer = new byte[2];
            await _networkStream.ReadExactlyAsync(sizeBuffer, cancellationToken);

            // convert ke integer panjang payload
            int dataSize = NbConvert.BytesToHex(sizeBuffer).HexToInt();

            // --- baca payload sesuai ukuran ---
            byte[] dataBuffer = new byte[dataSize];
            await _networkStream.ReadExactlyAsync(dataBuffer, cancellationToken);

            return dataBuffer;
        }

        public void Close()
        {
            _networkStream.Close();
            _client.Close();
        }
    }
}