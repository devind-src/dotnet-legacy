using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.VaAccounts
{
    /// <summary>Active defaults to false client-side — mirrors legacy's VaAccount() constructor
    /// (status = "0" / Not Active) rather than the "always default active" convention used by
    /// most other modules in this project.</summary>
    public class CreateVaAccountRequest
    {
        [Required, MaxLength(30)]
        public string AccNr { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string VaName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string GroupName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? VaNotes { get; set; }

        public bool Active { get; set; }

        public decimal LowBalance { get; set; }
        public decimal MinBalance { get; set; }
    }
}
