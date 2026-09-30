using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using SyncNetPro.Sdk.Configuration;

namespace SyncNetPro.Sdk.Tracing;

/// <summary>
/// Trace ke RabbitMQ (default exchange, routing key = queue, persistent, <c>application/json</c>) —
/// perilaku sama dengan <c>RabbitLogWorker</c> SDK lama, termasuk cooldown koneksi ulang 10 detik.
/// </summary>
public sealed class RabbitMqTraceSink(RabbitMqSettings settings, ILogger logger, TimeProvider? time = null) : ITraceSink
{
    private static readonly TimeSpan ReconnectInterval = TimeSpan.FromSeconds(10);
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private IConnection? _connection;
    private IChannel? _channel;
    private DateTimeOffset _lastAttempt = DateTimeOffset.MinValue;

    /// <inheritdoc />
    public string Name => $"RabbitMQ ({settings.HostName}:{settings.Port}/{settings.QueueName})";

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task<bool> TrySendAsync(TraceRecord record, string json, CancellationToken cancellationToken)
    {
        if (!await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false)) return false;

        try
        {
            var properties = new BasicProperties { Persistent = true, ContentType = "application/json" };
            await _channel!.BasicPublishAsync(string.Empty, settings.QueueName, false, properties,
                Encoding.UTF8.GetBytes(json), cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is RabbitMQClientException or AlreadyClosedException or IOException)
        {
            logger.LogWarning("Publish trace ke RabbitMQ gagal: {Error}", ex.Message);
            return false;
        }
    }

    private async Task<bool> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true }) return true;
        if (_time.GetUtcNow() - _lastAttempt < ReconnectInterval) return false;
        _lastAttempt = _time.GetUtcNow();

        try
        {
            await CleanupAsync().ConfigureAwait(false);
            var factory = new ConnectionFactory { HostName = settings.HostName, Port = settings.Port };
            _connection = await factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await _channel.QueueDeclareAsync(settings.QueueName, durable: true, exclusive: false, autoDelete: false,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Terkoneksi ke RabbitMQ {Host}:{Port}", settings.HostName, settings.Port);
            return true;
        }
        catch (Exception ex) when (ex is RabbitMQClientException or BrokerUnreachableException or IOException or OperationInterruptedException)
        {
            logger.LogWarning("Koneksi RabbitMQ gagal: {Error}", ex.Message);
            return false;
        }
    }

    private async Task CleanupAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync().ConfigureAwait(false);
        if (_connection is not null) await _connection.DisposeAsync().ConfigureAwait(false);
        _channel = null;
        _connection = null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await CleanupAsync().ConfigureAwait(false);
}
