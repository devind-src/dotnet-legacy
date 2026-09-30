namespace SyncNetPro.Sdk.Configuration;

/// <summary>Pengaturan RabbitMQ dari config Core.</summary>
/// <param name="HostName">Host.</param>
/// <param name="Port">Port.</param>
/// <param name="QueueName">Nama queue trace.</param>
public sealed record RabbitMqSettings(string HostName, int Port, string QueueName);

/// <summary>Lingkungan runtime hasil gabungan opsi interface dan konfigurasi Core.</summary>
public sealed record CoreEnvironment
{
    /// <summary>Folder instalasi.</summary>
    public required SyncNetPaths Paths { get; init; }

    /// <summary>Connection string PostgreSQL; <c>null</c> bila tidak ada database (mode JSON tanpa config Core).</summary>
    public string? ConnectionString { get; init; }

    /// <summary>Folder log file.</summary>
    public required string LogDirectory { get; init; }

    /// <summary>Folder trace fallback.</summary>
    public required string TraceDirectory { get; init; }

    /// <summary>RabbitMQ bila diaktifkan di config Core.</summary>
    public RabbitMqSettings? RabbitMq { get; init; }

    /// <summary>URL SyncNetHsm dari config Core.</summary>
    public string? HsmUrl { get; init; }

    /// <summary><c>true</c> bila config Core ditemukan dan dibaca.</summary>
    public bool CoreConfigurationLoaded { get; init; }
}
