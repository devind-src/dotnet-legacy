using Newtonsoft.Json;

namespace SyncNetPro.Contracts;

/// <summary>Rincian fee transaksi. JSON: <c>fee_data</c>.</summary>
public sealed class Fees
{
    /// <summary>JSON: <c>total_fee</c>.</summary>
    [JsonProperty("total_fee", Order = 1)] public decimal TotalFee { get; set; }

    /// <summary>JSON: <c>acquirer_fee</c>.</summary>
    [JsonProperty("acquirer_fee", Order = 2)] public decimal AcquirerFee { get; set; }

    /// <summary>JSON: <c>merchant_fee</c>.</summary>
    [JsonProperty("merchant_fee", Order = 3)] public decimal MerchantFee { get; set; }

    /// <summary>JSON: <c>submerchant_fee</c>.</summary>
    [JsonProperty("submerchant_fee", Order = 4)] public decimal SubmerchantFee { get; set; }

    /// <summary>JSON: <c>switch_fee</c>.</summary>
    [JsonProperty("switch_fee", Order = 5)] public decimal SwitchFee { get; set; }

    /// <summary>JSON: <c>biller_fee</c>.</summary>
    [JsonProperty("biller_fee", Order = 6)] public decimal BillerFee { get; set; }

    /// <summary>JSON: <c>issuer_fee</c>.</summary>
    [JsonProperty("issuer_fee", Order = 7)] public decimal IssuerFee { get; set; }
}
