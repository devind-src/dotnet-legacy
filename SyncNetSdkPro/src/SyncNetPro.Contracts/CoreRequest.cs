using Newtonsoft.Json;

namespace SyncNetPro.Contracts;

/// <summary>
/// Request antara interface dan SyncNet Core.
/// <list type="bullet">
/// <item>Kanal outbound (Core SinkNode / <c>port_out</c>): dikirim Core ke interface.</item>
/// <item>Kanal inbound (Core SourceNode / <c>port_in</c>): dikirim interface ke Core.</item>
/// </list>
/// Nama dan urutan properti JSON dikunci (dok. 02 §2.1) — jangan menambah properti baru;
/// informasi tambahan wajib ditaruh di <see cref="AdditionalData"/>.
/// </summary>
public sealed class CoreRequest
{
    /// <summary>JSON: <c>pan</c>.</summary>
    [JsonProperty("pan", Order = 1)] public string? Pan { get; set; }

    /// <summary>Message type indicator (mis. <c>0200</c>).</summary>
    [JsonProperty("msgtype", Order = 2)] public string? MessageType { get; set; }

    /// <summary>Kode transaksi SyncNet, lihat <see cref="Contracts.TranType"/>.</summary>
    [JsonProperty("tran_type", Order = 3)] public string? TranType { get; set; }

    /// <summary>JSON: <c>tran_type_ext</c>.</summary>
    [JsonProperty("tran_type_ext", Order = 4)] public string? TranTypeExt { get; set; }

    /// <summary>JSON: <c>from_acc_type</c>.</summary>
    [JsonProperty("from_acc_type", Order = 5)] public string? FromAccountType { get; set; }

    /// <summary>JSON: <c>to_acc_type</c>.</summary>
    [JsonProperty("to_acc_type", Order = 6)] public string? ToAccountType { get; set; }

    /// <summary>JSON: <c>currency</c>.</summary>
    [JsonProperty("currency", Order = 7)] public string? Currency { get; set; }

    /// <summary>JSON: <c>amount_tran</c>.</summary>
    [JsonProperty("amount_tran", Order = 8)] public decimal Amount { get; set; }

    /// <summary>STAN. Bagian dari kunci korelasi Core.</summary>
    [JsonProperty("trace_number", Order = 9)] public string? TraceNumber { get; set; }

    /// <summary>Tanggal/jam transaksi. Bagian dari kunci korelasi Core.</summary>
    [JsonProperty("datetime_tran", Order = 10)] public string? TransactionDateTime { get; set; }

    /// <summary>JSON: <c>date_settle</c>.</summary>
    [JsonProperty("date_settle", Order = 11)] public string? SettlementDate { get; set; }

    /// <summary>JSON: <c>merchant_type</c>.</summary>
    [JsonProperty("merchant_type", Order = 12)] public string? MerchantType { get; set; }

    /// <summary>JSON: <c>merchant_id</c>.</summary>
    [JsonProperty("merchant_id", Order = 13)] public string? MerchantId { get; set; }

    /// <summary>Terminal ID. Bagian dari kunci korelasi Core.</summary>
    [JsonProperty("terminal_id", Order = 14)] public string? TerminalId { get; set; }

    /// <summary>JSON: <c>acq_inst_id</c>.</summary>
    [JsonProperty("acq_inst_id", Order = 15)] public string? AcquirerInstitutionId { get; set; }

    /// <summary>JSON: <c>fwd_inst_id</c>.</summary>
    [JsonProperty("fwd_inst_id", Order = 16)] public string? ForwardingInstitutionId { get; set; }

    /// <summary>Retrieval reference number.</summary>
    [JsonProperty("refnum", Order = 17)] public string? ReferenceNumber { get; set; }

    /// <summary>JSON: <c>pos_entry_mode</c>.</summary>
    [JsonProperty("pos_entry_mode", Order = 18)] public string? PosEntryMode { get; set; }

    /// <summary>JSON: <c>receiving_inst_id</c>.</summary>
    [JsonProperty("receiving_inst_id", Order = 19)] public string? ReceivingInstitutionId { get; set; }

    /// <summary>JSON: <c>from_acc_number</c>.</summary>
    [JsonProperty("from_acc_number", Order = 20)] public string? FromAccountNumber { get; set; }

    /// <summary>JSON: <c>to_acc_number</c>.</summary>
    [JsonProperty("to_acc_number", Order = 21)] public string? ToAccountNumber { get; set; }

    /// <summary>Data yang dikembalikan apa adanya pada response.</summary>
    [JsonProperty("echo_data", Order = 22)] public object? EchoData { get; set; }

    /// <summary>Data titipan yang tidak disimpan ke database.</summary>
    [JsonProperty("temp_data", Order = 23)] public object? TempData { get; set; }

    /// <summary>Kunci transaksi asal untuk reversal/advice.</summary>
    [JsonProperty("original_data", Order = 24)] public string? OriginalData { get; set; }

    /// <summary>Tempat seluruh informasi tambahan (keputusan Q1). Gunakan <see cref="AdditionalDataExtensions"/>.</summary>
    [JsonProperty("additional_data", Order = 25)] public Dictionary<string, object?>? AdditionalData { get; set; } = [];

    /// <summary>JSON: <c>fee_data</c>.</summary>
    [JsonProperty("fee_data", Order = 26)] public Fees? Fees { get; set; } = new();

    /// <summary>JSON: <c>security</c>.</summary>
    [JsonProperty("security", Order = 27)] public Security? Security { get; set; } = new();

    /// <summary>JSON: <c>private_data</c>.</summary>
    [JsonProperty("private_data", Order = 28)] public PrivateData? PrivateData { get; set; } = new();

    /// <summary>JSON: <c>virtual_account</c>.</summary>
    [JsonProperty("virtual_account", Order = 29)] public VirtualAccount? VirtualAccount { get; set; } = new();

    /// <summary>
    /// Membuat <see cref="CoreResponse"/> dari request ini (lihat <see cref="CoreResponse.From(CoreRequest)"/>)
    /// sekaligus mengisi kode respons.
    /// </summary>
    public CoreResponse ToResponse(string responseCode, string? responseMessage = null, string authorizedBy = AuthorizedBy.External)
    {
        var response = CoreResponse.From(this);
        response.ResponseCode = responseCode;
        response.ResponseMessage = responseMessage;
        response.AuthorizedBy = authorizedBy;
        return response;
    }
}
