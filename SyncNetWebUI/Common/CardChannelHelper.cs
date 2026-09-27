namespace SyncNetWasm.Common
{
    /// <summary>Shared "Channel" dropdown values hardcoded 3 times in the legacy Card module
    /// (Profile &gt; CardLimit/Override Limit, Profile &gt; Product &gt; Limit, and the read-only
    /// Queries &gt; Card inquiry out of this project's scope) — kept as one shared lookup here so
    /// Card &gt; Profile &gt; Product &gt; Limit and Card &gt; Profile &gt; Override Limit don't
    /// duplicate it.</summary>
    public static class CardChannelHelper
    {
        public static readonly Dictionary<string, string> Labels = new()
        {
            ["6010"] = "Teller",
            ["6011"] = "ATM",
            ["6012"] = "POS",
            ["6013"] = "Phone Banking",
            ["6014"] = "Internet Banking",
            ["6015"] = "Kiosk",
            ["6016"] = "Mobile Banking",
        };

        public static string Label(string? channel) => channel != null && Labels.TryGetValue(channel, out var label) ? $"{channel} — {label}" : (channel ?? "");
    }
}
