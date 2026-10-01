using Newtonsoft.Json;

namespace SyncNet.Template.Models;

// TODO(1): sesuaikan dengan spesifikasi API yang dipublikasikan ke mitra/aplikasi channel.

/// <summary>Request dari channel (POST /inquiry, /payment, /advice, /reversal).</summary>
public sealed class ChannelRequest
{
    [JsonProperty("reference")] public string? Reference { get; set; }

    [JsonProperty("trace_number")] public string? TraceNumber { get; set; }

    [JsonProperty("terminal_id")] public string? TerminalId { get; set; }

    [JsonProperty("merchant_id")] public string? MerchantId { get; set; }

    [JsonProperty("product_code")] public string? ProductCode { get; set; }

    [JsonProperty("customer_id")] public string? CustomerId { get; set; }

    [JsonProperty("amount")] public decimal Amount { get; set; }

    /// <summary>Advice/reversal: data transaksi asal (<c>original_data</c> Core).</summary>
    [JsonProperty("original_data")] public string? OriginalData { get; set; }
}

/// <summary>Response ke channel.</summary>
public sealed class ChannelResponse
{
    [JsonProperty("rc")] public string ResponseCode { get; set; } = string.Empty;

    [JsonProperty("message")] public string? Message { get; set; }

    [JsonProperty("reference", NullValueHandling = NullValueHandling.Ignore)] public string? Reference { get; set; }

    [JsonProperty("amount", NullValueHandling = NullValueHandling.Ignore)] public decimal? Amount { get; set; }

    [JsonProperty("fee", NullValueHandling = NullValueHandling.Ignore)] public decimal? Fee { get; set; }

    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)] public Dictionary<string, object?>? Data { get; set; }
}

/// <summary>Kode respons yang dibuat interface sendiri (tidak dari Core); format sama dengan ApiChannel lama.</summary>
public static class ChannelCodes
{
    public const string InvalidTransaction = "X3";
    public const string InvalidUrl = "X6";
    public const string AuthFailed = "X8";
    public const string BillerCutoff = "X15";
    public const string InvalidRequest = "30";
    public const string Timeout = "68";
    public const string LinkDown = "89";
}
