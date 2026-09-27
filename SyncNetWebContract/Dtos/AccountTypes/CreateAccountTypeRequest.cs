using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.AccountTypes
{
    /// <summary>acct_type is char(2), name is varchar(20) — real column widths.</summary>
    public class CreateAccountTypeRequest
    {
        [Required, MaxLength(2)]
        public string AcctType { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Name { get; set; } = string.Empty;
    }
}
