using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_cashout_bank" — menu "Cashout Account &gt; Fee Mini ATM"
    /// (`/cashout/account/miniatm`). `id` (bigint) **is** `GENERATED ALWAYS AS IDENTITY`
    /// (verified live via `information_schema.columns.identity_generation` — a plain
    /// `column_default` check is NOT enough to tell, since identity columns report a NULL
    /// `column_default` too; confirmed the hard way when a manual max(id)+1 insert failed with
    /// Postgres `428C9: cannot insert a non-DEFAULT value into column "id"`). Do not assign
    /// `id` manually. No FK constraints exist to sw_merchant_group/sw_submerchant_group;
    /// legacy validates group_name exists in either one manually before insert/update
    /// (replicated in the service layer). Widths verified live: group_name varchar(100),
    /// bank_code varchar(6), bank_name varchar(50), acc_number varchar(20), acc_name
    /// varchar(50). Legacy `IsValid()` never checks acc_name for presence — kept optional
    /// here too. Legacy also leaves every field (including group_name) editable on Update, and
    /// only checks the (group_name, bank_code, acc_number) duplicate combo on Create —
    /// replicated as-is.</summary>
    [Table("sw_cashout_bank", Schema = "public")]
    public class SwCashoutBank
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long id { get; set; }
        public string? group_name { get; set; }
        public string? bank_code { get; set; }
        public string? bank_name { get; set; }
        public string? acc_number { get; set; }
        public string? acc_name { get; set; }
    }
}
