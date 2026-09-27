using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.VaAccounts
{
    /// <summary>AccNr and GroupName are not included — immutable after create (readonly in the
    /// legacy Edit form). Balance is not included either — legacy's raw UPDATE never touches
    /// it; balance only changes via the Approval workflow.</summary>
    public class UpdateVaAccountRequest
    {
        [MaxLength(50)]
        public string? VaNotes { get; set; }

        public bool Active { get; set; }

        public decimal LowBalance { get; set; }
        public decimal MinBalance { get; set; }
    }
}
