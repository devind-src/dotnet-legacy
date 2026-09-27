using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductPostpaidBillers
{
    /// <summary>Node tidak bisa diubah (hapus lalu tambah biller lain). Mengubah priority biller
    /// alternate menjadi 1 memindahkan primary.</summary>
    public class UpdateProductPostpaidBillerRequest
    {
        [Required, Range(1, 32000)]
        public int Priority { get; set; }
        [Range(0, int.MaxValue)]
        public int? FeeSharing { get; set; }
        [Range(0, 100)]
        public int? LbWeight { get; set; }
        [MaxLength(50)]
        public string? Notes { get; set; }
        public bool Active { get; set; } = true;
    }
}
