namespace SyncNetApi.Common
{
    public static class StringExtensions
    {
        /// <summary>
        /// Defensive-in-depth for any query text that ends up interpolated into raw SQL.
        /// NOT actually required for EF Core LINQ queries (Where/Contains etc. are always
        /// parameterized automatically) — but if a future module ports over the legacy
        /// app's raw ADO.NET/Dapper queries, prefer parameterized queries there too rather
        /// than relying on this alone.
        /// </summary>
        public static string Sanitize(this string? data)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;
            return data.Replace("'", "").Replace("--", "").Replace(";", "").Replace("/*", "").Replace("*/", "");
        }

        /// <summary>Mirrors legacy MaskingPan (BIN 6 digits + "******" + last 4) — Queries &gt;
        /// Transaction (§7.27) applies this server-side in the DTO projection, never sending the
        /// full PAN over the wire at all. Unlike legacy Blazor Server (masks only at render time,
        /// after the full PAN already round-tripped through the component), this WASM app ships
        /// DTOs to the browser as JSON, so masking has to happen before serialization.</summary>
        public static string? MaskPan(this string? pan)
        {
            if (string.IsNullOrEmpty(pan)) return pan;
            if (pan.Length < 10) return pan;
            return pan.Substring(0, 6) + "******" + pan.Substring(pan.Length - 4, 4);
        }
    }
}
