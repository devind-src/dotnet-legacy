using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_agent_bank" — menu "Cashout Account &gt; QRIS Static"
    /// (`/cashout/account/qris`). `id` (bigint) **is** `GENERATED ALWAYS AS IDENTITY`
    /// (verified live) — do not assign manually. Widths verified live: nmid varchar(50),
    /// bank_code varchar(6), bank_name varchar(50), acc_number varchar(20) (legacy HTML form
    /// used maxlength=30, narrower than real column — not replicated), acc_name varchar(50).
    /// Legacy `IsValid()` requires Nmid/BankName/BankCode/AccNumber but never checks AccName
    /// for presence — kept optional here too, same pattern as Cashout &gt; Fee Mini ATM
    /// (SwCashoutBank). Only the (nmid, bank_code, acc_number) combo is checked for duplicates,
    /// and only on Create — never re-checked on Update. Every field (including nmid) stays
    /// editable on Update — no field is readonly on the legacy Edit form. NMID has no
    /// existence validation against any other table in the legacy Create/Update form (the
    /// `nmids` autocomplete datalist in Detail.razor is never actually populated — dead
    /// feature); `sw_store_nmid` looks like it was meant to be a master NMID list but is never
    /// wired up here (and is empty in production regardless) — out of scope, not replicated.
    /// Import CSV / Export CSV from the legacy Index/Import pages are intentionally out of
    /// scope for this port (confirmed with user) — only List/Create/Edit/Delete exist here.</summary>
    [Table("sw_agent_bank", Schema = "public")]
    public class SwAgentBank
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long id { get; set; }
        public string? nmid { get; set; }
        public string? bank_code { get; set; }
        public string? bank_name { get; set; }
        public string? acc_number { get; set; }
        public string? acc_name { get; set; }
    }
}
