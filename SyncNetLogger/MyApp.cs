using SyncNet.Common;
using SyncNet.DbRepository;
using SyncNet.Helpers;
using SyncNet.Library;
using SyncNet.Models;
using SyncNet.Networking;
using SyncNet.Services;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet
{
    public class MyApp
    {
        //constanta
        public const string APPNAME = "Log Services";
        public const string VERSION = "1.0";

        //variable
        private readonly XTcpListener _socket;
        private readonly DbMgr _dbMgr;

        private static ChannelService _channel;
        private static RabbitMQService _consumer;

        private static NbLogger _logger;

        public MyApp()
        {
            AppConfig.Initialize();

            _logger = new NbLogger(APPNAME);
            _channel = new ChannelService();
            _dbMgr = new DbMgr();

            _socket = new XTcpListener();
            _socket.OnConnect += SocketOnConnect;
            _socket.OnDisconnect += SocketOnDisconnect;
            _socket.OnDataArrival += SocketOnDataArrival;
            _socket.OnStopped += SocketOnStopped;
        }

        public async Task Start(CancellationToken stoppingToken)
        {
            try
            {
                //logger
                await Logger($"{APPNAME} start");

                //update status app
                await _dbMgr.UpdateStatusApp(DbMgr.EnumStatusApp.UP);

                //get port app
                int port = await _dbMgr.GetPortApplication();

                //listen
                await _socket.StartAsync(IPAddress.Any, port);

                await Logger($"Listening on port {port}");

                if (AppConfig.Settings.RabbitMQ.Enable == true)
                {
                    _consumer = new RabbitMQService();

                    // 1. Inisialisasi koneksi dan queue
                    await _consumer.InitializeAsync(stoppingToken);
                    await Logger("Connected to RabbitMQ");

                    // 2. Mulai mendengarkan pesan dari Queue
                    await _consumer.StartConsumingAsync(stoppingToken);
                    await Logger("Ready consume from RabbitMQ");
                }
            }
            catch (Exception ex) 
            {
                await Logger(ex.Message);
            }
        }
        public async Task Stop()
        {
            //logger
            await Logger($"{APPNAME} stop");

            //update status app
            await _dbMgr.UpdateStatusApp(DbMgr.EnumStatusApp.DOWN);

            //close
            await _socket.StopAsync();
            await _channel.StopAsync();
        }

        public static async Task Logger(string ErrMessage)
        {
            Console.WriteLine(ErrMessage);

            await _logger.LogAsync(ErrMessage);
        }
        public static async Task Logger(string ErrMessage, string Detail)
        {
            Console.WriteLine(ErrMessage);

            await _logger.LogAsync(ErrMessage, Detail);
        }

        private async Task SocketOnDataArrival(byte[] Data, EndPoint remoteEP)
        {
            try
            {
                await _channel.EnqueueTransactionAsync(Data);
            }
            catch (Exception ex)
            {
                await Logger(ex.Message);
            }
        }

        private async Task SocketOnDisconnect(EndPoint ep)
        {
            await Logger($"Disconnected from {NetHelper.GetRemoteEP(ep)}");
        }
        private async Task SocketOnConnect(EndPoint ep)
        {
            await Logger($"Receive connection from {NetHelper.GetRemoteEP(ep)}");
        }
        private async Task SocketOnStopped(string msg)
        {
            await Logger(msg);
        }
    }
}
