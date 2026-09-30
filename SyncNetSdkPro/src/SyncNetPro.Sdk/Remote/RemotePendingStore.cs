using System.Collections.Concurrent;

namespace SyncNetPro.Sdk.Remote;

/// <summary>Request remote yang menunggu balasan terkorelasi (per koneksi).</summary>
internal sealed class RemotePendingStore
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _pending = new(StringComparer.Ordinal);

    public int Count => _pending.Count;

    public TaskCompletionSource<byte[]> Register(string key, string connectionName)
    {
        var completion = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(key, completion))
        {
            throw new InvalidOperationException($"Request dengan kunci {key} masih menunggu balasan di koneksi {connectionName}.");
        }

        return completion;
    }

    public void Remove(string key, TaskCompletionSource<byte[]> completion) =>
        _pending.TryRemove(new KeyValuePair<string, TaskCompletionSource<byte[]>>(key, completion));

    public bool TryComplete(string key, byte[] payload) =>
        _pending.TryRemove(key, out TaskCompletionSource<byte[]>? completion) && completion.TrySetResult(payload);

    public void FailAll(Exception exception)
    {
        foreach (TaskCompletionSource<byte[]> completion in _pending.Values) completion.TrySetException(exception);
        _pending.Clear();
    }

    public static async Task<byte[]> AwaitAsync(TaskCompletionSource<byte[]> completion, TimeSpan timeout, TimeProvider time,
        string description, CancellationToken cancellationToken)
    {
        try
        {
            return await completion.Task.WaitAsync(timeout, time, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"Tidak ada balasan dari {description} dalam {timeout.TotalSeconds:0.#} detik.");
        }
    }
}
