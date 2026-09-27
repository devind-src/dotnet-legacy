using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.AgentBanks
{
    /// <summary>Every field is editable on Update, including Nmid — no field is readonly on
    /// the legacy Edit form. Duplicate-combo check is intentionally NOT re-run here — legacy
    /// only runs it on Create.</summary>
    public class UpdateAgentBankRequest
    {
        [Required, MaxLength(50)]
        public string Nmid { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string BankName { get; set; } = string.Empty;

        [Required, MaxLength(6)]
        public string BankCode { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string AccNumber { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? AccName { get; set; }
    }
}
