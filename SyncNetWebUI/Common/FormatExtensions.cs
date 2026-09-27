namespace SyncNetWasm.Common
{
    // Subset of SyncNetBlazorServer's Helpers/FormatHelper.cs + DataHelper.cs ported for the
    // Home dashboard — only the members that page actually uses.
    public static class FormatExtensions
    {
        public static string FormatAmount(this int amount) => amount.ToString("N0");
        public static string FormatAmount(this long amount) => amount.ToString("N0");
        public static string FormatAmount(this decimal amount) => amount.ToString("N2");

        public static string FormatAmountK(this long amount) => FormatAmountK(Convert.ToDecimal(amount));

        public static string FormatAmountK(this decimal amount)
        {
            if (amount < 1000) return amount.ToString("N0");

            if (amount < 1_000_000)
            {
                if (amount < 100_000) return amount.ToString("N0");

                var valueInK = amount / 1000;
                return valueInK >= 1000
                    ? ((int)(valueInK / 1000)).ToString() + "Jt"
                    : valueInK.ToString("N0") + "Rb";
            }

            var valueInM = amount / 1_000_000;
            return valueInM >= 1000
                ? ((int)(valueInM / 1000)).ToString() + "M"
                : valueInM.ToString("N0") + "Jt";
        }

        public static string FormatTime(this DateTime dt) => dt.ToString("HH:mm:ss.fff");

        public static string GetStatusNode(this string status, DateTime? lastEcho)
        {
            var result = status == "1" ? "OK" : "Critical";

            if (lastEcho != null)
            {
                var totalMinutes = DateTime.Now.Subtract(lastEcho.Value).TotalMinutes;
                if (status == "1" && totalMinutes > 10) result = "Warning";
            }

            return result;
        }
    }
}
