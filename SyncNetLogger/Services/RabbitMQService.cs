using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SyncNet.Common;
using SyncNet.Library;
using SyncNet.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet.Services
{
    public class RabbitMQService : IAsyncDisposable
    {
        private readonly string _hostname;
        private readonly string _queueName;
        private readonly int _port;
        private readonly NbTrace _trace;

        private IConnection _connection;
        private IChannel _channel; // Menggunakan IChannel, bukan IModel
        private bool _disposed;

        public RabbitMQService()
        {
            _trace = new NbTrace();

            _hostname = AppConfig.Settings.RabbitMQ.HostName ?? "localhost";
            _queueName = AppConfig.Settings.RabbitMQ.QueueName ?? "transaction_logs_queue";
            _port = AppConfig.Settings.RabbitMQ.Port;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _hostname,
                    Port = _port,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                // Pada v7+, pembuatan connection & channel bersifat async
                _connection = await factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                // Deklarasi Dead Letter Exchange & Queue khusus untuk menyimpan log yang gagal
                string dlxExchange = "transaction-log-dlx";
                string dlqName = "transaction-log-dead-letter-queue";
                string dlxRoutingKey = "dead-letter";

                await _channel.ExchangeDeclareAsync(
                    exchange: dlxExchange,
                    type: ExchangeType.Direct,
                    durable: true,
                    cancellationToken: cancellationToken);

                await _channel.QueueDeclareAsync(
                    queue: dlqName,// Target: "transaction-log-dead-letter-queue"
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: cancellationToken);

                await _channel.QueueBindAsync(
                    queue: dlqName,
                    exchange: dlxExchange,
                    routingKey: dlxRoutingKey,
                    cancellationToken: cancellationToken);

                // Deklarasi Main Queue dengan mengaitkan DLX
                var queueArgs = new Dictionary<string, object>
                {
                    { "x-single-active-consumer", true },
                    { "x-dead-letter-exchange", dlxExchange },       // Mengarahkan kegagalan ke DLX
                    { "x-dead-letter-routing-key", dlxRoutingKey }   // Routing key menuju DLQ
                };

                await _channel.QueueDeclareAsync(
                    queue: _queueName, // Target: "transaction-log-queue" (Main Queue)
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: queueArgs, // Mengaitkan Main Queue ke DLX
                    cancellationToken: cancellationToken);

                await _channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: 1,
                    global: false,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"Connect to {_hostname} RabbitMQ service failed. {ex.Message}");
            }
        }

        public async Task StartConsumingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var consumer = new AsyncEventingBasicConsumer(_channel);

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    byte[] body = ea.Body.ToArray();
                    string message = Encoding.UTF8.GetString(body);
                    ulong deliveryTag = ea.DeliveryTag;

                    bool isSuccess = await WriteLogToFileAsync(message, cancellationToken);

                    if (isSuccess)
                    {
                        await _channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        // Hitung sudah berapa kali pesan ini gagal diproses
                        int retryCount = GetRetryCount(ea.BasicProperties);
                        int maxRetry = 3;

                        if (retryCount < maxRetry)
                        {
                            MyApp.Logger($"Failed to write log. Attempt {retryCount + 1}/{maxRetry}. Requeueing...");

                            // Jeda 2 detik sebelum requeue untuk menghindari beban CPU 100%
                            await Task.Delay(2000, cancellationToken);

                            // requeue: true -> kembalikan ke antrean utama untuk dicoba lagi
                            await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken: cancellationToken);
                        }
                        else
                        {
                            MyApp.Logger($"Message failed to process after {maxRetry} attempts. Moving to Dead Letter Queue (DLQ)...");

                            // requeue: false -> RabbitMQ otomatis memindahkan pesan ini ke Dead Letter Queue (DLQ)
                            await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
                        }
                    }
                };

                await _channel.BasicConsumeAsync(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: cancellationToken);
            }
            catch
            {
                throw new Exception("Consuming RabbitMQ service failed");
            }
        }

        private async Task<bool> WriteLogToFileAsync(string logMessage, CancellationToken cancellationToken)
        {
            try
            {
                var req = JsonConvert.DeserializeObject<LogModel>(logMessage);
                string detail = req.Detail == null ? "" : detail = req.Detail.ToString();

                //write data
                if (req.LogType == LogType.Info)
                    await _trace.WriteTraceStatusAsync(req.Datetime, req.AppName, req.FileName, req.Title, detail);
                else
                    await _trace.WriteTraceMessageAsync(req.Datetime, req.AppName, req.FileName, req.Title, detail);

                return true;
            }
            catch (Exception ex)
            {
                MyApp.Logger($"WriteLogToFileAsync error: {ex.Message}");
                return false;
            }
        }

        // Fungsi Helper untuk membaca header x-death bawaan RabbitMQ
        private int GetRetryCount(IReadOnlyBasicProperties properties)
        {
            if (properties?.Headers != null && properties.Headers.TryGetValue("x-death", out var xDeathObj))
            {
                if (xDeathObj is System.Collections.IList xDeathList && xDeathList.Count > 0)
                {
                    if (xDeathList[0] is System.Collections.IDictionary deathEntry)
                    {
                        if (deathEntry.Contains("count"))
                        {
                            return Convert.ToInt32(deathEntry["count"]);
                        }
                    }
                }
            }

            return 0; // Pertama kali diproses (belum pernah di-requeue)
        }
        
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            if (_channel != null) await _channel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();

            _disposed = true;
        }
    }
}