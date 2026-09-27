namespace SyncNetApi.Dtos.Routing
{
    /// <summary>Cara memilih biller per produk. Nilainya sama dengan SDK (SyncNet.Constants.RoutingMode):
    /// bill payment &amp; purchase disimpan di sw_fees.routing_mode, topup di sw_routes_margin.routing_mode.</summary>
    public static class RoutingModes
    {
        public const string Static = "STATIC";
        public const string Priority = "PRIORITY";
        public const string BestPrice = "BEST_PRICE";
        public const string LoadBalance = "LOAD_BALANCE";

        public static readonly IReadOnlyList<string> All = [Static, Priority, BestPrice, LoadBalance];

        /// <summary>Mode dynamic memilih biller per siklus dan melewati biller yang diblokir.</summary>
        public static bool IsDynamic(string? mode) => mode is Priority or BestPrice or LoadBalance;

        public static bool IsValid(string? mode) => mode is Static or Priority or BestPrice or LoadBalance;

        public static string Label(string? mode) => mode switch
        {
            Priority => "Priority",
            BestPrice => "Best Price",
            LoadBalance => "Load Balance",
            Static => "Static",
            _ => "Ikuti Default"
        };
    }
}
