namespace SyncNetPro.Sdk.Hosting;

/// <summary>
/// Modul tambahan (mis. <c>SyncNetPro.Routing</c>) yang mengikuti siklus hidup interface. Daftarkan sebagai singleton
/// <c>ISyncNetModule</c>; runtime memanggilnya berurutan sesuai registrasi.
/// </summary>
public interface ISyncNetModule
{
    /// <summary>Setelah konfigurasi node dimuat, sebelum kanal Core dibuka (transaksi pertama sudah melihat data modul).</summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>Saat RESYNC, setelah konfigurasi dimuat ulang dan sebelum <see cref="SyncNetInterface.OnConfigurationReloadedAsync"/>.</summary>
    Task ReloadAsync(CancellationToken cancellationToken);

    /// <summary>Setelah kanal Core dan koneksi eksternal ditutup (tidak ada transaksi yang masih berjalan).</summary>
    Task StopAsync(CancellationToken cancellationToken);
}
