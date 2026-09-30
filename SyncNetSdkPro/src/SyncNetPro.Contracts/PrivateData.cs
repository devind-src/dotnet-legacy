using Newtonsoft.Json;

namespace SyncNetPro.Contracts;

/// <summary>
/// Data internal SDK ↔ Core. JSON: <c>private_data</c>. Hanya subset milik interface;
/// properti tambahan milik Core diabaikan saat deserialisasi (lihat dok. 02 §2.3).
/// </summary>
public sealed class PrivateData
{
    /// <summary>JSON: <c>sink_node</c>.</summary>
    [JsonProperty("sink_node", Order = 1)] public string? SinkNode { get; set; }

    /// <summary>IP klien eksternal (diisi SDK pada kanal inbound).</summary>
    [JsonProperty("ip_external", Order = 2)] public string? IpExternal { get; set; }

    /// <summary>Nama koneksi eksternal asal (diisi SDK pada kanal inbound, dikembalikan Core).</summary>
    [JsonProperty("connection_name", Order = 3)] public string? ConnectionName { get; set; }

    /// <summary>JSON: <c>retry_send</c>.</summary>
    [JsonProperty("retry_send", Order = 4)] public int RetrySend { get; set; }
}
