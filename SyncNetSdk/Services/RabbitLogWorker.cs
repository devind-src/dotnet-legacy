using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RabbitMQ.Client;
using SyncNet.Common;
using SyncNet.Helpers;
using SyncNet.Models;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SyncNet.Services
{
    internal class RabbitLogWorker : BackgroundService
    {
        private readonly string _hostname;
        private readonly string _queueName;
        private readonly int _port;

        private static readonly SemaphoreSlim _fileLock = new(1, 1);
        private readonly Channel<string> _channel;

        private IConnection _connection;
        private IChannel _rabbitChannel;

        // Cooldown reconnect untuk mencegah connection storm
        private DateTime _lastConnectionAttempt = DateTime.MinValue;
        private readonly TimeSpan _reconnectInterval = TimeSpan.FromSeconds(10);

        public RabbitLogWorker(Channel<string> channel)
        {
            _channel = channel;

            _hostname = SdkConfig.Settings.RabbitMQ.HostName ?? "localhost";
            _queueName = SdkConfig.Settings.RabbitMQ.QueueName ?? "transaction_logs_queue";
            _port = SdkConfig.Settings.RabbitMQ.Port;
        }

        private async Task<bool> EnsureRabbitMQConnectedAsync(CancellationToken cancellationToken)
        {
            if (_connection != null && _connection.IsOpen && _rabbitChannel != null && _rabbitChannel.IsOpen)
                return true;

            // Jika baru saja gagal connect, tahan dulu (cooldown) agar tidak membebani CPU
            if (DateTime.UtcNow - _lastConnectionAttempt < _reconnectInterval)
            {
                return false;
            }

            _lastConnectionAttempt = DateTime.UtcNow;

            try
            {
                // Bersihkan koneksi lama jika ada yang terputus sebagian
                await CleanupRabbitMQResourcesAsync();

                var factory = new ConnectionFactory
                {
                    HostName = _hostname,
                    Port = _port
                };
                _connection = await factory.CreateConnectionAsync(cancellationToken);

                // Buat IChannel RabbitMQ
                _rabbitChannel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                // Pastikan Queue sudah terdeklarasi di RabbitMQ
                await _rabbitChannel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: cancellationToken);

                await AppProcessor.Logger("Connected to RabbitMQ");
                return true;
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger($"EnsureRabbitMQConnectedAsync error: {ex.Message}");
                return false;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await AppProcessor.Logger("RabbitMQ worker running");

            // Membaca dari buffer channel secara kontinu
            await foreach (var log in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                await ProcessMessageAsync(log, stoppingToken);
            }

            await AppProcessor.Logger("RabbitMQ worker stop");
        }

        private async Task ProcessMessageAsync(string logMessage, CancellationToken cancellationToken)
        {
            bool isConnected = await EnsureRabbitMQConnectedAsync(cancellationToken);

            if (isConnected && _rabbitChannel != null && _rabbitChannel.IsOpen)
            {
                try
                {
                    byte[] body = Encoding.UTF8.GetBytes(logMessage);
                    var properties = new BasicProperties
                    {
                        Persistent = true,
                        ContentType = "application/json"
                    };

                    await _rabbitChannel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: _queueName,
                        mandatory: false,
                        basicProperties: properties,
                        body: body,
                        cancellationToken: cancellationToken);

                    return; // Berhasil terkirim ke RabbitMQ
                }
                catch (Exception ex)
                {
                    await AppProcessor.Logger($"ProcessMessageAsync error: {ex.Message}");
                }
            }

            // Fallback jika RabbitMQ Down atau Publish Error
            await WriteFallbackLog(logMessage);
        }

        private async Task WriteFallbackLog(string logModel)
        {
            try
            {
                var req = JsonConvert.DeserializeObject<LogModel>(logModel);

                string appName = req.AppName.AdjustFileName();
                string logName = req.FileName.AdjustFileName();
                string logDate = req.Datetime.ToString("yyyyMMdd_HH");

                string targetDir = Path.Combine(SdkConfig.TraceDir, appName);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                string filename = Path.Combine(targetDir, $"{logName}_{logDate}.log");

                string dt = req.Datetime.ToString("[dd MMM yyyy HH:mm:ss.fff] ");
                string detail = req.Detail == null ? string.Empty : req.Detail.ToString();
                string log = string.IsNullOrEmpty(detail)
                    ? dt + req.Title
                    : dt + req.Title + Environment.NewLine + req.Detail + Environment.NewLine;

                // Lock penulisan file agar aman dari race condition
                await _fileLock.WaitAsync();
                try
                {
                    // Write log ke file lokal dengan retry mechanism
                    for (int i = 0; i < SdkConfig.Settings.RetryPolicy.MaxRetryLog; i++)
                    {
                        try
                        {
                            using var fs = new StreamWriter(filename, true);
                            await fs.WriteLineAsync(log);

                            break;
                        }
                        catch
                        {
                            await Task.Delay(50); // Jeda singkat sebelum retry}
                        }
                    }
                }
                finally
                {
                    _fileLock.Release();
                }
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger($"WriteFallbackLog error: {ex.Message}");
            }
        }

        private async Task CleanupRabbitMQResourcesAsync()
        {
            try
            {
                if (_rabbitChannel != null)
                {
                    await _rabbitChannel.CloseAsync();
                    _rabbitChannel.Dispose();
                }

                if (_connection != null)
                {
                    await _connection.CloseAsync();
                    _connection.Dispose();
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            finally
            {
                _rabbitChannel = null;
                _connection = null;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await AppProcessor.Logger("RabbitMQ worker shutdown");

            await CleanupRabbitMQResourcesAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
