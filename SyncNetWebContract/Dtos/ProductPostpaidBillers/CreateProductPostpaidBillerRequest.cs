using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductPostpaidBillers
{
    /// <summary>Biller pertama produk otomatis menjadi primary. Priority kosong = urutan
    /// berikutnya; priority 1 pada produk yang sudah punya primary memindahkan primary.</summary>
    public class CreateProductPostpaidBillerRequest
    {
        [Required, MaxLength(11)]
        public string ProductId { get; set; } = string.Empty;
        [Required]
        public int NodeId { get; set; }
        [Range(1, 32000)]
        public int? Priority { get; set; }
        [Range(0, int.MaxValue)]
        public int? FeeSharing { get; set; }
        [Range(0, 100)]
        public int? LbWeight { get; set; }
        [MaxLength(50)]
        public string? Notes { get; set; }
        public bool Active { get; set; } = true;
    }
}
