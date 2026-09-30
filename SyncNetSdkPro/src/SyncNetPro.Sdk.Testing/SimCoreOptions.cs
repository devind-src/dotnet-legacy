using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Testing;

/// <summary>Konfigurasi SimCore (file <c>simcore.json</c>).</summary>
public sealed class SimCoreOptions
{
    /// <summary>Nama aplikasi interface yang disimulasikan (informasi).</summary>
    public string AppName { get; set; } = "SimCore";

    /// <summary>Alamat bind semua listener SimCore.</summary>
    public string BindAddress { get; set; } = "127.0.0.1";

    /// <summary>Node yang disimulasikan (setara <c>sw_nodes</c>).</summary>
    public List<SimNodeOptions> Nodes { get; set; } = [];

    /// <summary>Port Log Services tiruan (null = tidak aktif, 0 = port acak).</summary>
    public int? LogServicesPort { get; set; }

    /// <summary>Command port interface (untuk <c>simcore cmd</c> dan skenario command).</summary>
    public SimCommandTarget? Interface { get; set; }

    /// <summary>Stub sistem eksternal (biller/bank) untuk uji end-to-end di satu mesin.</summary>
    public List<RemoteStubOptions> RemoteStubs { get; set; } = [];

    /// <summary>Jumlah pesan terakhir yang disimpan di memori untuk UI/API.</summary>
    public int HistoryLimit { get; set; } = 2000;

    internal static readonly JsonSerializerSettings JsonSettings = new()
    {
        Converters = { new StringEnumConverter() },
        MissingMemberHandling = MissingMemberHandling.Error,
        // Jangan mengisi ulang objek default yang sudah ada (mis. TcpHeaderOptions) — selalu buat objek baru.
        ObjectCreationHandling = ObjectCreationHandling.Replace,
    };

    /// <summary>Membaca <c>simcore.json</c>.</summary>
    /// <exception cref="InvalidDataException">File tidak valid.</exception>
    public static SimCoreOptions Load(string path)
    {
        try
        {
            SimCoreOptions options = JsonConvert.DeserializeObject<SimCoreOptions>(File.ReadAllText(path), JsonSettings)
                ?? throw new InvalidDataException("File kosong.");
            options.Validate();
            return options;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{path} tidak valid: {ex.Message}", ex);
        }
    }

    /// <summary>Validasi konfigurasi.</summary>
    public void Validate()
    {
        var duplicates = Nodes.GroupBy(n => n.Name, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0) throw new InvalidDataException($"Nama node duplikat: {string.Join(", ", duplicates)}");
        if (Nodes.Any(n => string.IsNullOrWhiteSpace(n.Name))) throw new InvalidDataException("Setiap node wajib memiliki Name.");
    }

    /// <summary>Serialisasi (untuk <c>simcore init</c>).</summary>
    public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented, JsonSettings);
}

/// <summary>Node yang disimulasikan.</summary>
public sealed class SimNodeOptions
{
    /// <summary>Nama node (sama dengan <c>SyncNet:Nodes[].Name</c> di interface).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Kategori: Merchant = hanya source, BillerIssuer = hanya sink, Both = keduanya.</summary>
    public NodeCategory Category { get; set; } = NodeCategory.Both;

    /// <summary>Port kanal source (interface → Core). 0 = port acak.</summary>
    public int PortIn { get; set; }

    /// <summary>Port kanal sink (Core → interface). 0 = port acak.</summary>
    public int PortOut { get; set; }

    /// <summary>Batas waktu respons interface (detik).</summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>Batas waktu untuk ADV/REV (detik).</summary>
    public int AdviceTimeoutSeconds { get; set; } = 30;

    /// <summary>Kirim reversal otomatis saat request (selain ADV/REV) timeout, seperti Core dengan <c>auto_reversal</c>.</summary>
    public bool AutoReversal { get; set; }

    /// <summary>Cara SimCore membalas request pada kanal source.</summary>
    public SourceResponderOptions Responder { get; set; } = new();
}

/// <summary>Mode balasan kanal source.</summary>
public enum SourceResponseMode
{
    /// <summary>Balas <c>request.ToResponse(ResponseCode)</c>.</summary>
    Fixed,

    /// <summary>Aturan pertama yang cocok menentukan balasan; tidak cocok → <see cref="SourceResponderOptions.ResponseCode"/>.</summary>
    Rules,

    /// <summary>Tidak membalas (uji timeout di interface).</summary>
    None,
}

/// <summary>Balasan SimCore untuk request dari interface (kanal source).</summary>
public sealed class SourceResponderOptions
{
    /// <summary>Mode.</summary>
    public SourceResponseMode Mode { get; set; } = SourceResponseMode.Fixed;

    /// <summary>Kode respons default.</summary>
    public string ResponseCode { get; set; } = "00";

    /// <summary>Pesan respons default.</summary>
    public string ResponseMessage { get; set; } = "Approved";

    /// <summary>Jeda sebelum membalas (ms).</summary>
    public int DelayMs { get; set; }

    /// <summary>Aturan untuk <see cref="SourceResponseMode.Rules"/>.</summary>
    public List<ResponseRule> Rules { get; set; } = [];
}

/// <summary>Aturan balasan berdasarkan isi request.</summary>
public sealed class ResponseRule
{
    /// <summary><c>tran_type</c> yang cocok (null = semua).</summary>
    public string? TranType { get; set; }

    /// <summary><c>tran_type_ext</c> yang cocok (null = semua).</summary>
    public string? TranTypeExt { get; set; }

    /// <summary>Nominal minimum (inklusif).</summary>
    public decimal? MinAmount { get; set; }

    /// <summary>Nominal maksimum (inklusif).</summary>
    public decimal? MaxAmount { get; set; }

    /// <summary>Kode respons.</summary>
    public string ResponseCode { get; set; } = "00";

    /// <summary>Pesan respons.</summary>
    public string? ResponseMessage { get; set; }

    /// <summary>Jeda (ms); null = jeda default.</summary>
    public int? DelayMs { get; set; }

    /// <summary>Isi tambahan untuk <c>additional_data</c>.</summary>
    public Dictionary<string, object?>? AdditionalData { get; set; }
}

/// <summary>Command port interface.</summary>
public sealed class SimCommandTarget
{
    /// <summary>Host.</summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>Port.</summary>
    public int Port { get; set; }
}

/// <summary>Jenis stub sistem eksternal.</summary>
public enum RemoteStubType
{
    /// <summary>Server TCP (interface sebagai klien).</summary>
    Tcp,

    /// <summary>Server HTTP (interface sebagai klien HTTP).</summary>
    Http,
}

/// <summary>Stub sistem eksternal.</summary>
public sealed class RemoteStubOptions
{
    /// <summary>Nama stub.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Jenis.</summary>
    public RemoteStubType Type { get; set; } = RemoteStubType.Tcp;

    /// <summary>Port (0 = acak).</summary>
    public int Port { get; set; }

    /// <summary>Header TCP; null = tanpa header.</summary>
    public TcpHeaderOptions? Header { get; set; } = new();

    /// <summary>Balasan; aturan pertama yang cocok dipakai.</summary>
    public List<StubReply> Replies { get; set; } = [];
}

/// <summary>Aturan balasan stub.</summary>
public sealed class StubReply
{
    /// <summary>Cocok bila request (teks UTF-8) mengandung nilai ini.</summary>
    public string? Contains { get; set; }

    /// <summary>Cocok bila request (hex) diawali nilai ini.</summary>
    public string? HexPrefix { get; set; }

    /// <summary>HTTP: path yang cocok (mis. <c>/bill/inquiry</c>).</summary>
    public string? Path { get; set; }

    /// <summary>Balasan teks.</summary>
    public string? Reply { get; set; }

    /// <summary>Balasan hex (TCP biner).</summary>
    public string? ReplyHex { get; set; }

    /// <summary>Kembalikan request apa adanya.</summary>
    public bool Echo { get; set; }

    /// <summary>Status HTTP.</summary>
    public int Status { get; set; } = 200;

    /// <summary>Jeda (ms).</summary>
    public int DelayMs { get; set; }

    /// <summary>Tidak membalas sama sekali.</summary>
    public bool Silent { get; set; }
}
