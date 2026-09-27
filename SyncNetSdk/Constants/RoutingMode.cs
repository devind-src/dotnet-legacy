namespace SyncNet.Constants
{
    // cara memilih biller untuk sebuah produk.
    // bill payment & purchase: sw_fees.routing_mode (baris default produk atau baris per CA).
    // topup: sw_routes_margin.routing_mode.
    public class RoutingMode
    {
        public const string STATIC = "STATIC";              // satu biller tetap, tanpa failover
        public const string PRIORITY = "PRIORITY";          // priority terkecil yang sehat
        public const string BEST_PRICE = "BEST_PRICE";      // bill payment: sharing fee terbesar, topup: margin terbesar
        public const string LOAD_BALANCE = "LOAD_BALANCE";  // acak berbobot di antara biller yang sehat

        // mode dynamic = memilih biller per siklus dan melewati biller yang diblokir
        public static bool IsDynamic(string mode)
        {
            return mode == PRIORITY || mode == BEST_PRICE || mode == LOAD_BALANCE;
        }

        // nilai kosong atau tidak dikenal diperlakukan sebagai STATIC
        public static string Normalize(string mode)
        {
            return IsDynamic(mode) == true ? mode : STATIC;
        }
    }
}
