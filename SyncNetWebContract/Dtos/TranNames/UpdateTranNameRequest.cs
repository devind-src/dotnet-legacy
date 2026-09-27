using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.TranNames
{
    /// <summary>TransCode is immutable after create — only TransName/Active can change.
    /// trans_name is varchar(50) — real column width.</summary>
    public class UpdateTranNameRequest
    {
        [Required, MaxLength(50)]
        public string TransName { get; set; } = string.Empty;

        public bool Active { get; set; } = true;
    }
}
