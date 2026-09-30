using System.ComponentModel.DataAnnotations;
using SyncNetPro.Sdk.Nodes;

namespace SyncNetPro.Sdk;

/// <summary>Sumber konfigurasi node/koneksi.</summary>
public enum NodeSourceKind
{
    /// <summary>Tabel <c>sw_app</c>, <c>sw_nodes</c>, <c>sw_connections</c> di database Core (produksi).</summary>
    Database,

    /// <summary>Bagian <c>SyncNet:Nodes</c> di appsettings (pengembangan / SimCore, tanpa database).</summary>
    Json,
}

/// <summary>Tujuan pengiriman trace.</summary>
public enum TraceSinkKind
{
    /// <summary>RabbitMQ bila diaktifkan di config Core; selain itu Log Services TCP bila terdaftar; selain itu file.</summary>
    Auto,

    /// <summary>Log Services via TCP.</summary>
    LogServices,

    /// <summary>RabbitMQ (pengaturan dari config Core).</summary>
    RabbitMq,

    /// <summary>File lokal di direktori trace.</summary>
    File,

    /// <summary>Trace tidak dikirim ke mana pun.</summary>
    None,
}

/// <summary>Konfigurasi interface (bagian <c>SyncNet</c> di appsettings).</summary>
public sealed class SyncNetOptions
{
    /// <summary>Nama bagian konfigurasi.</summary>
    public const string SectionName = "SyncNet";

    /// <summary>Nama aplikasi, sama dengan <c>sw_app.app_name</c> / <c>sw_nodes.app_name</c>. Wajib.</summary>
    [Required(AllowEmptyStrings = false)]
    public string AppName { get; set; } = string.Empty;

    /// <summary>Versi yang dibalas command <c>VERSION</c>. Default: versi assembly entry.</summary>
    public string? Version { get; set; }

    /// <summary>Folder instalasi SyncNet. Default: env <c>SYNCNET_HOME</c>, lalu <c>C:\SyncNet</c> / <c>/opt/SyncNet</c>.</summary>
    public string? Home { get; set; }

    /// <summary>Sumber konfigurasi node.</summary>
    public NodeSourceKind NodeSource { get; set; } = NodeSourceKind.Database;

    /// <summary>Definisi node untuk <see cref="NodeSourceKind.Json"/>.</summary>
    public List<NodeInfo> Nodes { get; set; } = [];

    /// <summary>Definisi koneksi eksternal untuk <see cref="NodeSourceKind.Json"/>.</summary>
    public List<RemoteConnectionInfo> Connections { get; set; } = [];

    /// <summary>Kanal ke Core.</summary>
    public CoreChannelOptions Core { get; set; } = new();

    /// <summary>Command port.</summary>
    public CommandOptions Command { get; set; } = new();

    /// <summary>Koneksi ke sistem eksternal.</summary>
    public RemoteOptions Remote { get; set; } = new();

    /// <summary>Trace ke Log Services.</summary>
    public TraceOptions Trace { get; set; } = new();

    /// <summary>Log file lokal.</summary>
    public FileLogOptions Logging { get; set; } = new();

    /// <summary>Database.</summary>
    public DatabaseOptions Database { get; set; } = new();

    /// <summary>Batas request Core yang diproses bersamaan per node.</summary>
    [Range(1, 10_000)]
    public int MaxConcurrentRequestsPerNode { get; set; } = 100;
}

/// <summary>Opsi kanal Core.</summary>
public sealed class CoreChannelOptions
{
    /// <summary>Host Core. Default <c>127.0.0.1</c> (sama dengan SDK lama).</summary>
    [Required(AllowEmptyStrings = false)]
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>Jeda koneksi ulang ke Core (SDK lama: 5 detik).</summary>
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Tambahan waktu di atas <c>request_timeout</c>/<c>advice_timeout</c> node saat menunggu respons Core.</summary>
    public TimeSpan ResponseTimeoutMargin { get; set; } = TimeSpan.FromSeconds(2);
}

/// <summary>Opsi koneksi ke sistem eksternal.</summary>
public sealed class RemoteOptions
{
    /// <summary>
    /// Terima sertifikat TLS yang tidak valid (self-signed/dev). Default <c>false</c> — SDK lama selalu menerima
    /// (bug B6). Aktifkan hanya untuk lingkungan pengembangan.
    /// </summary>
    public bool AllowUntrustedCertificates { get; set; }

    /// <summary>Jeda sebelum <see cref="SyncNetInterface.OnAutoSignOnAsync"/> setelah terkoneksi (SDK lama: 3 detik).</summary>
    public TimeSpan AutoSignOnDelay { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>Batas waktu handshake TCP.</summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Interval pembaruan status koneksi ke database (SDK lama: 1 menit).</summary>
    public TimeSpan StatusInterval { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>Opsi command port.</summary>
public sealed class CommandOptions
{
    /// <summary>Aktifkan command server.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Alamat bind (default semua interface, sama dengan SDK lama).</summary>
    public string BindAddress { get; set; } = "0.0.0.0";

    /// <summary>Port; default dari <c>sw_app.command_port</c>. Wajib diisi pada mode JSON.</summary>
    [Range(0, 65535)]
    public int? Port { get; set; }
}

/// <summary>Opsi trace.</summary>
public sealed class TraceOptions
{
    /// <summary>Status awal trace (dapat diubah dengan command <c>TRACE ON/OFF</c>).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Tujuan trace.</summary>
    public TraceSinkKind Sink { get; set; } = TraceSinkKind.Auto;

    /// <summary>Host Log Services; default dari <c>sw_app</c> baris <c>Log Services</c>.</summary>
    public string? LogServicesHost { get; set; }

    /// <summary>Port Log Services; default dari <c>sw_app</c> baris <c>Log Services</c>.</summary>
    public int? LogServicesPort { get; set; }

    /// <summary>Kapasitas antrean trace di memori.</summary>
    [Range(100, 10_000_000)]
    public int QueueCapacity { get; set; } = 100_000;
}

/// <summary>Opsi log file lokal.</summary>
public sealed class FileLogOptions
{
    /// <summary>Tulis log ke file.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Folder log; default dari config Core (<c>Paths.*.Logs</c>) atau <c>{Home}/Logs</c>.</summary>
    public string? Directory { get; set; }

    /// <summary>Folder trace fallback; default dari config Core (<c>Paths.*.Traces</c>) atau <c>{Home}/Traces</c>.</summary>
    public string? TraceDirectory { get; set; }

    /// <summary>Teruskan log Information+ ke Log Services sebagai trace info (perilaku <c>AppProcessor.Logger</c> lama).</summary>
    public bool ForwardToTrace { get; set; } = true;

    /// <summary>Pertahankan nama file log gaya lama di Windows (tanpa normalisasi huruf kecil).</summary>
    public bool LegacyWindowsFileNames { get; set; }
}

/// <summary>Opsi database.</summary>
public sealed class DatabaseOptions
{
    /// <summary>Connection string PostgreSQL. Default dibentuk dari config Core (password didekripsi).</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Laporkan status aplikasi/node/koneksi ke database.</summary>
    public bool ReportStatus { get; set; } = true;
}
