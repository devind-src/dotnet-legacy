using Newtonsoft.Json;

namespace SyncNetPro.Contracts;

/// <summary>
/// Response antara interface dan SyncNet Core. Nama dan urutan properti JSON dikunci
/// (dok. 02 §2.2). Core mengkorelasikan response dengan request melalui
/// <c>tran_type + datetime_tran + trace_number + terminal_id</c> — keempat field ini
/// jangan diubah.
/// </summary>
public sealed class CoreResponse
{
    /// <summary>JSON: <c>pan</c>.</summary>
    [JsonProperty("pan", Order = 1)] public string? Pan { get; set; }

    /// <summary>JSON: <c>msgtype</c>.</summary>
    [JsonProperty("msgtype", Order = 2)] public string? MessageType { get; set; }

    /// <summary>JSON: <c>tran_type</c>.</summary>
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

    /// <summary>JSON: <c>trace_number</c>.</summary>
    [JsonProperty("trace_number", Order = 9)] public string? TraceNumber { get; set; }

    /// <summary>JSON: <c>datetime_tran</c>.</summary>
    [JsonProperty("datetime_tran", Order = 10)] public string? TransactionDateTime { get; set; }

    /// <summary>JSON: <c>date_settle</c>.</summary>
    [JsonProperty("date_settle", Order = 11)] public string? SettlementDate { get; set; }

    /// <summary>JSON: <c>merchant_type</c>.</summary>
    [JsonProperty("merchant_type", Order = 12)] public string? MerchantType { get; set; }

    /// <summary>JSON: <c>merchant_id</c>.</summary>
    [JsonProperty("merchant_id", Order = 13)] public string? MerchantId { get; set; }

    /// <summary>JSON: <c>terminal_id</c>.</summary>
    [JsonProperty("terminal_id", Order = 14)] public string? TerminalId { get; set; }

    /// <summary>JSON: <c>acq_inst_id</c>.</summary>
    [JsonProperty("acq_inst_id", Order = 15)] public string? AcquirerInstitutionId { get; set; }

    /// <summary>JSON: <c>fwd_inst_id</c>.</summary>
    [JsonProperty("fwd_inst_id", Order = 16)] public string? ForwardingInstitutionId { get; set; }

    /// <summary>JSON: <c>refnum</c>.</summary>
    [JsonProperty("refnum", Order = 17)] public string? ReferenceNumber { get; set; }

    /// <summary>JSON: <c>pos_entry_mode</c>.</summary>
    [JsonProperty("pos_entry_mode", Order = 18)] public string? PosEntryMode { get; set; }

    /// <summary>JSON: <c>receiving_inst_id</c>.</summary>
    [JsonProperty("receiving_inst_id", Order = 19)] public string? ReceivingInstitutionId { get; set; }

    /// <summary>JSON: <c>from_acc_number</c>.</summary>
    [JsonProperty("from_acc_number", Order = 20)] public string? FromAccountNumber { get; set; }

    /// <summary>JSON: <c>to_acc_number</c>.</summary>
    [JsonProperty("to_acc_number", Order = 21)] public string? ToAccountNumber { get; set; }

    /// <summary>JSON: <c>additional_amount</c>.</summary>
    [JsonProperty("additional_amount", Order = 22)] public string? AdditionalAmount { get; set; }

    /// <summary>Kode respons (mis. <c>00</c>).</summary>
    [JsonProperty("resp_code", Order = 23)] public string? ResponseCode { get; set; }

    /// <summary>JSON: <c>resp_message</c>.</summary>
    [JsonProperty("resp_message", Order = 24)] public string? ResponseMessage { get; set; }

    /// <summary>Pihak yang memutuskan respons, lihat <see cref="Contracts.AuthorizedBy"/>. Default <c>"1"</c> (external).</summary>
    [JsonProperty("authorized_by", Order = 25)] public string? AuthorizedBy { get; set; } = Contracts.AuthorizedBy.External;

    /// <summary>JSON: <c>echo_data</c>.</summary>
    [JsonProperty("echo_data", Order = 26)] public object? EchoData { get; set; }

    /// <summary>JSON: <c>temp_data</c>.</summary>
    [JsonProperty("temp_data", Order = 27)] public object? TempData { get; set; }

    /// <summary>JSON: <c>original_data</c>.</summary>
    [JsonProperty("original_data", Order = 28)] public string? OriginalData { get; set; }

    /// <summary>JSON: <c>additional_data</c>.</summary>
    [JsonProperty("additional_data", Order = 29)] public Dictionary<string, object?>? AdditionalData { get; set; } = [];

    /// <summary>JSON: <c>fee_data</c>.</summary>
    [JsonProperty("fee_data", Order = 30)] public Fees? Fees { get; set; } = new();

    /// <summary>JSON: <c>security</c>.</summary>
    [JsonProperty("security", Order = 31)] public Security? Security { get; set; } = new();

    /// <summary>JSON: <c>private_data</c>.</summary>
    [JsonProperty("private_data", Order = 32)] public PrivateData? PrivateData { get; set; } = new();

    /// <summary>JSON: <c>virtual_account</c>.</summary>
    [JsonProperty("virtual_account", Order = 33)] public VirtualAccount? VirtualAccount { get; set; } = new();

    /// <summary>
    /// Membuat response dari request (dok. 02 §7, keputusan Q2):
    /// <list type="bullet">
    /// <item>Menyalin field identitas transaksi, <c>echo_data</c>, <c>original_data</c>,
    /// <c>additional_data</c>, <c>fee_data</c>, <c>security</c>, <c>private_data</c>, <c>virtual_account</c>.</item>
    /// <item><c>msgtype</c> diisi MTI response (<see cref="MessageTypes.ToResponse(string?)"/>).</item>
    /// <item><c>pos_entry_mode</c> disalin dari request.</item>
    /// <item><c>temp_data</c> tidak disalin; <c>authorized_by</c> = external.</item>
    /// </list>
    /// Sub-objek disalin (bukan dibagi referensinya) sehingga mengubah response tidak mengubah request.
    /// </summary>
    public static CoreResponse From(CoreRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CoreResponse
        {
            Pan = request.Pan,
            MessageType = MessageTypes.ToResponse(request.MessageType),
            TranType = request.TranType,
            TranTypeExt = request.TranTypeExt,
            FromAccountType = request.FromAccountType,
            ToAccountType = request.ToAccountType,
            Currency = request.Currency,
            Amount = request.Amount,
            TraceNumber = request.TraceNumber,
            TransactionDateTime = request.TransactionDateTime,
            SettlementDate = request.SettlementDate,
            MerchantType = request.MerchantType,
            MerchantId = request.MerchantId,
            TerminalId = request.TerminalId,
            AcquirerInstitutionId = request.AcquirerInstitutionId,
            ForwardingInstitutionId = request.ForwardingInstitutionId,
            ReferenceNumber = request.ReferenceNumber,
            PosEntryMode = request.PosEntryMode,
            ReceivingInstitutionId = request.ReceivingInstitutionId,
            FromAccountNumber = request.FromAccountNumber,
            ToAccountNumber = request.ToAccountNumber,
            EchoData = request.EchoData,
            OriginalData = request.OriginalData,
            AdditionalData = request.AdditionalData is null ? null : new Dictionary<string, object?>(request.AdditionalData),
            Fees = Copy(request.Fees),
            Security = Copy(request.Security),
            PrivateData = Copy(request.PrivateData),
            VirtualAccount = Copy(request.VirtualAccount),
            AuthorizedBy = Contracts.AuthorizedBy.External,
        };
    }

    private static Fees? Copy(Fees? s) => s is null ? null : new Fees
    {
        TotalFee = s.TotalFee, AcquirerFee = s.AcquirerFee, MerchantFee = s.MerchantFee,
        SubmerchantFee = s.SubmerchantFee, SwitchFee = s.SwitchFee, BillerFee = s.BillerFee, IssuerFee = s.IssuerFee,
    };

    private static Security? Copy(Security? s) => s is null ? null : new Security
    {
        Track2Data = s.Track2Data, IccData = s.IccData, PinData = s.PinData, MiscData = s.MiscData,
        HsmCommand = s.HsmCommand, IsPinChange = s.IsPinChange, IsDebitTransaction = s.IsDebitTransaction,
    };

    private static PrivateData? Copy(PrivateData? s) => s is null ? null : new PrivateData
    {
        SinkNode = s.SinkNode, IpExternal = s.IpExternal, ConnectionName = s.ConnectionName, RetrySend = s.RetrySend,
    };

    private static VirtualAccount? Copy(VirtualAccount? s) => s is null ? null : new VirtualAccount
    {
        Enable = s.Enable, AccountNumber = s.AccountNumber, Amount = s.Amount, Balance = s.Balance,
    };
}
