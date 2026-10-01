using Newtonsoft.Json;

namespace SyncNet.Template.Models;

// TODO(1): sesuaikan dengan dokumen API biller (nama properti JSON, tipe, field wajib).

/// <summary>Request ke biller.</summary>
public sealed class BillerRequest
{
    [JsonProperty("partner_id")] public string? PartnerId { get; set; }

    [JsonProperty("reference")] public string? Reference { get; set; }

    [JsonProperty("trace_number")] public string? TraceNumber { get; set; }

    [JsonProperty("terminal_id")] public string? TerminalId { get; set; }

    [JsonProperty("product_code")] public string? ProductCode { get; set; }

    [JsonProperty("customer_id")] public string? CustomerId { get; set; }

    [JsonProperty("amount")] public decimal Amount { get; set; }

    [JsonProperty("original_reference")] public string? OriginalReference { get; set; }
}

/// <summary>Response biller.</summary>
public sealed class BillerResponse
{
    [JsonProperty("rc")] public string? ResponseCode { get; set; }

    [JsonProperty("message")] public string? Message { get; set; }

    [JsonProperty("customer_name")] public string? CustomerName { get; set; }

    [JsonProperty("amount")] public decimal? Amount { get; set; }

    [JsonProperty("bill_info")] public string? BillInfo { get; set; }
}
