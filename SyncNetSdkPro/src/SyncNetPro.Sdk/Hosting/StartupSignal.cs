namespace SyncNetPro.Sdk.Hosting;

/// <summary>Sinyal bahwa konfigurasi node pertama kali sudah dimuat (dipakai layanan background lain).</summary>
public sealed class StartupSignal
{
    private readonly TaskCompletionSource _loaded = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Selesai setelah konfigurasi dimuat.</summary>
    public Task ConfigurationLoaded => _loaded.Task;

    internal void SetConfigurationLoaded() => _loaded.TrySetResult();

    internal void SetFailed(Exception exception) => _loaded.TrySetException(exception);
}
