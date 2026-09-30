using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace SyncNetPro.Sdk.Transport;

/// <summary>
/// Satu koneksi TCP yang sudah terbentuk, dengan framing via <see cref="ITcpFrameCodec"/>.
/// Pengiriman di-serialisasi (satu frame utuh per penulisan).
/// </summary>
public sealed class FramedConnection : IAsyncDisposable
{
    private readonly Socket _socket;
    private readonly NetworkStream _stream;
    private readonly ITcpFrameCodec _codec;
    private readonly TimeSpan _partialFrameTimeout;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private int _disposed;

    internal FramedConnection(Socket socket, ITcpFrameCodec codec, TimeSpan partialFrameTimeout)
    {
        _socket = socket;
        _socket.NoDelay = true;
        _stream = new NetworkStream(socket, ownsSocket: true);
        _codec = codec;
        _partialFrameTimeout = partialFrameTimeout;
        RemoteEndPoint = socket.RemoteEndPoint!;
        LocalEndPoint = socket.LocalEndPoint!;
    }

    /// <summary>Alamat lawan.</summary>
    public EndPoint RemoteEndPoint { get; }

    /// <summary>Alamat lokal.</summary>
    public EndPoint LocalEndPoint { get; }

    /// <summary><c>true</c> selama koneksi belum ditutup.</summary>
    public bool IsOpen => Volatile.Read(ref _disposed) == 0 && _socket.Connected;

    /// <summary>Mengirim satu payload (dibungkus header oleh codec).</summary>
    /// <exception cref="ObjectDisposedException">Koneksi sudah ditutup.</exception>
    public async Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        byte[] frame = _codec.Encode(payload.Span);

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// Membaca frame sampai koneksi ditutup lawan, dibatalkan, atau data rusak.
    /// Setiap frame diteruskan ke <paramref name="onFrame"/> secara berurutan.
    /// </summary>
    internal async Task ReceiveLoopAsync(Func<byte[], Task> onFrame, CancellationToken cancellationToken)
    {
        var reader = PipeReader.Create(_stream, new StreamPipeReaderOptions(leaveOpen: true));
        try
        {
            bool partial = false;
            while (!cancellationToken.IsCancellationRequested)
            {
                ReadResult result;
                if (partial)
                {
                    // Sisa frame harus tiba dalam batas waktu (setara ReadTimeoutMs SDK lama).
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeout.CancelAfter(_partialFrameTimeout);
                    try
                    {
                        result = await reader.ReadAsync(timeout.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        throw new TimeoutException($"Frame tidak lengkap dalam {_partialFrameTimeout.TotalMilliseconds:0} ms dari {RemoteEndPoint}.");
                    }
                }
                else
                {
                    result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                }

                ReadOnlySequence<byte> buffer = result.Buffer;
                try
                {
                    while (_codec.TryDecode(ref buffer, out byte[] payload))
                    {
                        await onFrame(payload).ConfigureAwait(false);
                    }

                    partial = !buffer.IsEmpty;
                    if (result.IsCompleted) return;
                }
                finally
                {
                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
        }
        finally
        {
            await reader.CompleteAsync().ConfigureAwait(false);
        }
    }

    /// <summary>Menutup koneksi.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        try
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException)
        {
            // sudah terputus
        }
        catch (ObjectDisposedException)
        {
        }

        await _stream.DisposeAsync().ConfigureAwait(false);
        _sendLock.Dispose();
    }
}
