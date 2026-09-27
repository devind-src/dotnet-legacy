using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.AccountTypes
{
    /// <summary>AcctType is immutable after create — only Name/Active can change.
    /// name is varchar(20) — real column width.</summary>
    public class UpdateAccountTypeRequest
    {
        [Required, MaxLength(20)]
        public string Name { get; set; } = string.Empty;

        public bool Active { get; set; } = true;
    }
}
