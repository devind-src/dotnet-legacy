using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SyncNet.Common;
using SyncNet.Constants;
using SyncNet.DbRepository;
using SyncNet.Helpers;
using SyncNet.Library;
using SyncNet.Models.Common;
using SyncNet.Models.Networking;
using SyncNet.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SyncNet.Services
{
    internal class TcpLogQueueService : BackgroundService
    {
        private readonly Channel<string> _channel;        
        private XTcpClient _tcpClient;

        // Menyimpan waktu terakhir log error ditulis untuk throttling via Event Handler
        private DateTime _lastErrorLogTime = DateTime.MinValue;

        public TcpLogQueueService(Channel<string> channel)
        {
            _channel = channel;

            _tcpClient = new XTcpClient();
            _tcpClient.OnError += TcpClientOnError;
            _tcpClient.OnConnect += TcpClientOnConnect;
            _tcpClient.OnDisconnect += TcpClientOnDisconnect;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //get port command
            EndPointService ep = await DbMgr.GetEndPointLogServices();

            //koneksi internal retry setiap 5 detik
            _tcpClient.RetryDelaySeconds = 5;

            // Panggil ConnectAsync CUKUP SEKALI di awal.
            // Karena AutoReconnect = true, XTcpClient akan otomatis melakukan looping retry di background jika gagal.
            await _tcpClient.ConnectAsync("127.0.0.1", ep.Port, stoppingToken);

            // Loop membaca antrean data dari Channel
            await foreach (var log in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                bool isSent = false;

                // Loop while menjamin data log saat ini tidak akan dibuang/hilang sampai sukses terkirim
                while (!isSent && !stoppingToken.IsCancellationRequested)
                {
                    // Cek status koneksi menggunakan method bawaan XTcpClient
                    if (!_tcpClient.IsConnected())
                    {
                        // Tunda pembacaan selama 5 detik sebelum mengecek ulang status koneksi.
                        //await Task.Delay(5000, stoppingToken);

                        //Jika TCP mati, langsung alihkan log ini ke file lokal
                        await WriteFallbackLog(log);
                        isSent = true; // Set true untuk keluar dari loop while dan mengambil log berikutnya

                        continue;
                    }

                    try
                    {
                        // Kirim data payload log
                        await _tcpClient.SendAsync(log, stoppingToken);
                        isSent = true; // Set true untuk keluar dari loop while dan mengambil log berikutnya
                    }
                    catch 
                    {
                        // Terjadi kegagalan mendadak saat proses stream.write
                        await Task.Delay(2000, stoppingToken);
                    }
                }
            }
        }       

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // Lepas event handler untuk menghindari memory leak saat stop
            _tcpClient.OnError -= TcpClientOnError;
            _tcpClient.OnConnect -= TcpClientOnConnect;
            _tcpClient.OnDisconnect -= TcpClientOnDisconnect;

            await _tcpClient.DisconnectAsync();
            await base.StopAsync(cancellationToken);
        }

        private async Task WriteFallbackLog(string logModel)
        {
            var req = JsonConvert.DeserializeObject<LogModel>(logModel);

            string appName = req.AppName.AdjustFileName();
            string logName = req.FileName.AdjustFileName();
            string logDate = req.Datetime.ToString("yyyyMMdd_HH");
            string filename = $@"{AppConfig.TraceDir}/{appName}/{logName}_{logDate}.log";

            string dt = req.Datetime.ToString("[dd MMM yyyy HH:mm:ss.fff] ");
            string detail = req.Detail == null ? string.Empty : req.Detail.ToString();
            string log = string.IsNullOrEmpty(detail)
                ? dt + req.Title 
                : dt + req.Title + Environment.NewLine + req.Detail + Environment.NewLine;

            // Write log ke file lokal dengan retry mechanism
            for (int i = 0; i < AppConfig.MaxRetryLog; i++)
            {
                try
                {
                    var fs = new StreamWriter(filename, true);
                    fs.WriteLine(log);
                    fs.Close();

                    break;
                }
                catch { }
            }
        }

        private async Task TcpClientOnError(Exception ex)
        {
            // Throttling Log: Mencegah XTcpClient menulis log error bertubi-tubi dalam waktu sedetik.
            // Error hanya akan ditulis ke AppProcessor maksimal sekali setiap 5 detik.
            if ((DateTime.Now - _lastErrorLogTime).TotalSeconds >= 5)
            {
                _lastErrorLogTime = DateTime.Now;
                await MyApp.Logger($"Log service error: {ex.Message}");
            }
        }

        private async Task TcpClientOnDisconnect(EndPoint ep)
        {
            await MyApp.Logger($"Log service disconnected from {NetHelper.GetRemoteEP(ep)}");
        }

        private async Task TcpClientOnConnect(EndPoint ep)
        {
            await MyApp.Logger($"Log service connected to {NetHelper.GetRemoteEP(ep)}");
        }
    }
}
