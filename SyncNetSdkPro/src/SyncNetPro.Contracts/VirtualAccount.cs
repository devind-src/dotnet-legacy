using Newtonsoft.Json;

namespace SyncNetPro.Contracts;

/// <summary>Data virtual account. JSON: <c>virtual_account</c>.</summary>
public sealed class VirtualAccount
{
    /// <summary>JSON: <c>enable</c>.</summary>
    [JsonProperty("enable", Order = 1)] public bool Enable { get; set; }

    /// <summary>JSON: <c>acc_number</c>.</summary>
    [JsonProperty("acc_number", Order = 2)] public string? AccountNumber { get; set; }

    /// <summary>JSON: <c>amount</c>.</summary>
    [JsonProperty("amount", Order = 3)] public decimal Amount { get; set; }

    /// <summary>JSON: <c>balance</c>.</summary>
    [JsonProperty("balance", Order = 4)] public decimal Balance { get; set; }
}
