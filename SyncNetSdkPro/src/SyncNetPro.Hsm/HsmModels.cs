using Newtonsoft.Json;

namespace SyncNetPro.Hsm;

/// <summary>Hasil panggilan SyncNetHsm. Gagal jaringan/format dilaporkan sebagai <c>96</c> (seperti SDK lama), bukan exception.</summary>
public record HsmResult
{
    /// <summary>Kode sukses SyncNetHsm.</summary>
    public const string Success = "00";

    /// <summary>Kode gagal (HSM/jaringan tidak tersedia).</summary>
    public const string Failed = "96";

    /// <summary><c>resp_code</c>.</summary>
    [JsonProperty("resp_code")]
    public string ResponseCode { get; init; } = Failed;

    /// <summary><c>resp_message</c>.</summary>
    [JsonProperty("resp_message")]
    public string? ResponseMessage { get; init; }

    /// <summary><c>resp_code == "00"</c>.</summary>
    [JsonIgnore]
    public bool IsSuccess => ResponseCode == Success;
}

/// <summary>Hasil generate/translate key.</summary>
public sealed record HsmKeyResult : HsmResult
{
    /// <summary><c>key_under_zmk</c> (generate key node).</summary>
    [JsonProperty("key_under_zmk")]
    public string? KeyUnderZmk { get; init; }

    /// <summary><c>key_under_tmk</c> (generate key terminal).</summary>
    [JsonProperty("key_under_tmk")]
    public string? KeyUnderTmk { get; init; }

    /// <summary><c>key_check_value</c>.</summary>
    [JsonProperty("key_check_value")]
    public string? KeyCheckValue { get; init; }
}

/// <summary>Hasil translate PIN block.</summary>
public sealed record HsmPinResult : HsmResult
{
    /// <summary><c>dest_pinblock</c>.</summary>
    [JsonProperty("dest_pinblock")]
    public string? DestinationPinBlock { get; init; }
}

/// <summary>Opsi klien (<c>SyncNet:Hsm</c>).</summary>
public sealed class HsmOptions
{
    /// <summary>Nama section konfigurasi.</summary>
    public const string SectionName = "SyncNet:Hsm";

    /// <summary>URL SyncNetHsm, mis. <c>http://127.0.0.1:40001</c>. Kosong = dari config Core (<c>Hsm:Url</c>).</summary>
    public string? Url { get; set; }

    /// <summary>Timeout per panggilan (default 5 detik, sama dengan SDK lama).</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>Request body — nama dan urutan properti sama dengan kelas internal HsmService lama (termasuk nilai null).</summary>
internal static class HsmRequests
{
    public sealed record GenerateKey(
        [property: JsonProperty("node_name")] string? NodeName,
        [property: JsonProperty("terminal_id")] string? TerminalId);

    public sealed record UpdateKey(
        [property: JsonProperty("node_name")] string NodeName,
        [property: JsonProperty("zpk_under_zmk")] string KeyUnderZmk);

    public sealed record TranslatePinBlock(
        [property: JsonProperty("node_source")] string SourceNode,
        [property: JsonProperty("node_dest")] string DestinationNode,
        [property: JsonProperty("source_pinblock")] string PinBlock,
        [property: JsonProperty("account_number")] string AccountNumber);

    public sealed record TranslateTerminalPinBlock(
        [property: JsonProperty("terminal_id")] string TerminalId,
        [property: JsonProperty("node_dest")] string DestinationNode,
        [property: JsonProperty("source_pinblock")] string PinBlock,
        [property: JsonProperty("account_number")] string AccountNumber);
}

/// <summary>Path SyncNetHsm (<c>HsmPath</c> SDK lama).</summary>
public static class HsmPaths
{
    /// <summary>Generate key node.</summary>
    public const string GenerateKey = "/hsm/generate-key";

    /// <summary>Generate key terminal.</summary>
    public const string GenerateKeyTerminal = "/hsm/generate-key-terminal";

    /// <summary>Translate key (ZPK under ZMK → LMK).</summary>
    public const string TranslateKey = "/hsm/translate-key";

    /// <summary>Translate PIN block antar node.</summary>
    public const string TranslatePinBlock = "/hsm/translate-pinblock";

    /// <summary>Translate PIN block dari terminal.</summary>
    public const string TranslatePinBlockTerminal = "/hsm/translate-pinblock-terminal";
}
