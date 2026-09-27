using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Banks
{
    /// <summary>Id is NOT supplied by the caller — computed server-side as max(id)+1 (sw_bank.id
    /// is not identity). bank is varchar(50), cbc is varchar(6) — real column widths.</summary>
    public class CreateBankRequest
    {
        [Required, MaxLength(50)]
        public string Bank { get; set; } = string.Empty;

        [MaxLength(6)]
        public string? Cbc { get; set; }
    }
}
