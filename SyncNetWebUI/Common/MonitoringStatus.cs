namespace SyncNetWasm.Common
{
    /// <summary>Status-label/color logic for Monitoring &gt; Realtime pages, ported field-for-field
    /// from legacy DataHelper.cs (GetStatusApp/GetStatusNode/GetProtocol/GetConnType) so the
    /// "traffic light" reads the same way it did in SyncNetBlazorServer.</summary>
    public static class MonitoringStatus
    {
        public static bool IsAppRunning(string? status) => status == "1";

        public static string AppStatusLabel(string? status) => status == "1" ? "Running" : "Down";

        public static string AppTypeLabel(string? appType) => appType == "1" ? "Interface" : "Core";

        /// <summary>OK (green) when status flag is "1" and last echo is recent; Warning (orange)
        /// when flagged up but hasn't echoed in over 10 minutes; Critical (red) otherwise.</summary>
        public static string NodeStatusLabel(string? status, DateTime? lastEcho)
        {
            var result = status == "1" ? "OK" : "Critical";

            if (lastEcho != null)
            {
                var totalMinutes = (DateTime.Now - lastEcho.Value).TotalMinutes;
                if (status == "1" && totalMinutes > 10) result = "Warning";
            }

            return result;
        }

        public static string ProtocolLabel(string? protocol) => protocol switch
        {
            "0" or "1" or "2" or "3" or "4" or "5" => "TCP",
            "6" => "MS Queue",
            "7" => "Web Service",
            "8" => "Custom",
            _ => "Unknown"
        };

        public static string ConnTypeLabel(string? connType) => connType == "0" ? "Server" : "Client";

        /// <summary>Legacy Connection monitoring only flags TCP connections as down when
        /// `remote == 0` — Message Queue/Web Service/Custom connections are always shown green
        /// (no remote-socket concept for them).</summary>
        public static bool IsConnectionDown(string? protocol, short? remote) => ProtocolLabel(protocol) == "TCP" && remote == 0;
    }
}
