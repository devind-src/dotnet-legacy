namespace SyncNetWasm.Common
{
    /// <summary>Label mapping for Queries &gt; Transaction/User Log — ported from legacy
    /// DataHelper.cs (GetSourceTranName/GetAuthTranName/GetLoginState).</summary>
    public static class QueryStatus
    {
        public static string SourceTranLabel(string? sourceTran) => sourceTran == "0" ? "Internal" : "External";

        public static string AuthTranLabel(string? authTran) => authTran == "0" ? "Internal" : "External";

        public static string LoginStateLabel(string? state) => state == "1" ? "Login" : "Logout";

        /// <summary>Shortens a raw User-Agent string to "{Browser} on {OS}" for the User Log
        /// list — the full string is still available via tooltip. Best-effort only (covers the
        /// common desktop browsers/OSes and curl/Postman for API smoke tests); falls back to the
        /// raw string if nothing recognizable matches.</summary>
        public static string FriendlyUserAgent(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent)) return "-";

            var ua = userAgent;
            string browser =
                ua.Contains("Edg/") ? "Edge" :
                ua.Contains("Chrome/") ? "Chrome" :
                ua.Contains("Firefox/") ? "Firefox" :
                ua.Contains("Safari/") && !ua.Contains("Chrome/") ? "Safari" :
                ua.Contains("curl/") ? "curl" :
                ua.Contains("PostmanRuntime") ? "Postman" :
                "Browser lain";

            string os =
                ua.Contains("Windows") ? "Windows" :
                ua.Contains("Mac OS X") ? "macOS" :
                ua.Contains("Linux") ? "Linux" :
                ua.Contains("Android") ? "Android" :
                ua.Contains("iPhone") || ua.Contains("iPad") ? "iOS" :
                null;

            if (browser is "curl" or "Postman") return browser;
            return os == null ? browser : $"{browser} di {os}";
        }
    }
}
