using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.TranNames
{
    /// <summary>trans_code is char(2), trans_name is varchar(50) — real column widths.</summary>
    public class CreateTranNameRequest
    {
        [Required, MaxLength(2)]
        public string TransCode { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string TransName { get; set; } = string.Empty;
    }
}
