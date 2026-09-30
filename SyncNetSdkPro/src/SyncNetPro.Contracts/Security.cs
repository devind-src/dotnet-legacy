using Newtonsoft.Json;

namespace SyncNetPro.Contracts;

/// <summary>Data keamanan (track2, ICC, PIN). JSON: <c>security</c>.</summary>
public sealed class Security
{
    /// <summary>JSON: <c>track2data</c>.</summary>
    [JsonProperty("track2data", Order = 1)] public string? Track2Data { get; set; }

    /// <summary>JSON: <c>iccdata</c>.</summary>
    [JsonProperty("iccdata", Order = 2)] public string? IccData { get; set; }

    /// <summary>JSON: <c>pindata</c>.</summary>
    [JsonProperty("pindata", Order = 3)] public string? PinData { get; set; }

    /// <summary>JSON: <c>miscdata</c>.</summary>
    [JsonProperty("miscdata", Order = 4)] public string? MiscData { get; set; }

    /// <summary>JSON: <c>hsm_cmd</c>.</summary>
    [JsonProperty("hsm_cmd", Order = 5)] public HsmCommand HsmCommand { get; set; }

    /// <summary>JSON: <c>is_pin_change</c>.</summary>
    [JsonProperty("is_pin_change", Order = 6)] public bool IsPinChange { get; set; }

    /// <summary>Default <c>true</c> (sama dengan SDK lama).</summary>
    [JsonProperty("is_debet_tran", Order = 7)] public bool IsDebitTransaction { get; set; } = true;
}
