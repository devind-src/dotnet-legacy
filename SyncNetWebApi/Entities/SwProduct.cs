using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_product" (tunggal — legacy [Table] attribute benar; legacy audit
    /// label salah pakai jamak "sw_products", bug yang sama seperti Country/dst di Phase 5a,
    /// tidak direplikasi) — menu "Product &gt; Master &gt; Product" (menu_id 1912, `/product/master/product`), DI
    /// LUAR grup "Configuration" tapi dibangun sebagai prasyarat minimal untuk validasi
    /// `product_id` di Job Fee (Phase 5e). Hanya modul Master yang dibangun — submenu Product
    /// lain (Category, Fees, Prices, Merchant, Mapping, Transfer) di luar scope permintaan ini.
    /// product_code adalah PK string manual (varchar(15)). **10 baris data produksi nyata
    /// sudah ada** (mis. TSel, ISAT, XL, Emoney Bank Mandiri) — hati-hati saat smoke test,
    /// pakai baris throwaway terpisah. `category` di sini plain text bebas (bukan dropdown FK
    /// ke sw_product_category — itu di luar scope minimal ini).</summary>
    [Table("sw_product", Schema = "public")]
    public class SwProduct
    {
        [Key]
        public string product_code { get; set; } = string.Empty;
        public string? product_name { get; set; }
        public string? category { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
