using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductPrepaidPricing
{
    /// <summary>Atur Routing Mode produk topup. StaticNodeId hanya untuk STATIC (supplier yang
    /// punya harga untuk produk ini); null = margin terbesar.</summary>
    public class UpdateTopupRoutingRequest
    {
        [Required, MaxLength(12)]
        public string RoutingMode { get; set; } = "STATIC";
        public int? StaticNodeId { get; set; }
    }
}
