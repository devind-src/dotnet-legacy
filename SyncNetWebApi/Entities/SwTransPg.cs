using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_trans_pg" — the core switch transaction ledger, shared with the live
    /// switching engine and with Queries &gt; Transaction. NOT a Virtual Account-owned table —
    /// mapped here only because VA &gt; Approval inserts one row per approved
    /// topup/adjustment (mirrors legacy VaTranRequestApproval), and VA &gt; Statement reads
    /// from it (join to va_account). tran_nr is bigint identity BY DEFAULT (verified live) —
    /// never assign manually.
    /// <br/><br/>
    /// <b>Bug fixed vs. legacy</b>: legacy's raw INSERT SQL (DbSwitchNetService.
    /// VaTranRequestApproval) writes columns named "fee_biller"/"fee_issuer", but the real
    /// columns are "fee_bil"/"fee_iss" (confirmed live via information_schema — legacy's SQL
    /// would fail with "column does not exist" if that path ever actually ran against this
    /// schema). Property names below use the correct real column names; user-confirmed fix
    /// during VA planning.
    /// <br/><br/>
    /// Originally only the columns needed for VA Approval/Statement were mapped here; Queries &gt;
    /// Transaction (§7.27) added msgtype/resp_code_rev/resp_code_adv/pos_entry_mode/icc_data/
    /// fee_mer/fee_sub for the read-only detail view (pure mapping addition, no migration —
    /// these are real pre-existing columns). track2data/pan_encrypted/track2data_encrypted stay
    /// unmapped on purpose — PCI-sensitive and never shown by the legacy detail view either.</summary>
    [Table("sw_trans_pg", Schema = "public")]
    public class SwTransPg
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long tran_nr { get; set; }

        [MaxLength(50)]
        public string? switch_key { get; set; }

        [MaxLength(20)]
        public string? source_node { get; set; }

        [MaxLength(20)]
        public string? dest_node { get; set; }

        public short? state { get; set; }

        [MaxLength(19)]
        public string? pan { get; set; }

        [MaxLength(3)]
        public string? tran_type { get; set; }

        [MaxLength(4)]
        public string? tran_type_ext { get; set; }

        [MaxLength(2)]
        public string? from_acc_type { get; set; }

        [MaxLength(2)]
        public string? to_acc_type { get; set; }

        public decimal? amount_tran_req { get; set; }
        public decimal? amount_tran_rsp { get; set; }
        public decimal? amount_va { get; set; }

        public decimal? fee_total { get; set; }
        public decimal? fee_acq { get; set; }
        public decimal? fee_swt { get; set; }
        public decimal? fee_bil { get; set; }
        public decimal? fee_iss { get; set; }

        [MaxLength(14)]
        public string? tran_datetime { get; set; }

        [MaxLength(8)]
        public string? date_settle_req { get; set; }

        [MaxLength(8)]
        public string? date_settle_rsp { get; set; }

        [MaxLength(12)]
        public string? trace_number { get; set; }

        [MaxLength(11)]
        public string? acq_inst_id { get; set; }

        [MaxLength(11)]
        public string? fwd_inst_id { get; set; }

        [MaxLength(50)]
        public string? track2data { get; set; }

        [MaxLength(30)]
        public string? reff_number { get; set; }

        [MaxLength(4)]
        public string? merchant_type { get; set; }

        [MaxLength(16)]
        public string? terminal_id { get; set; }

        [MaxLength(15)]
        public string? merchant_id { get; set; }

        [MaxLength(3)]
        public string? currency { get; set; }

        public decimal? last_balance { get; set; }

        [MaxLength(20)]
        public string? receiving_inst_id { get; set; }

        [MaxLength(28)]
        public string? from_acc_number { get; set; }

        [MaxLength(28)]
        public string? to_acc_number { get; set; }

        [MaxLength(30)]
        public string? va_acc_number { get; set; }

        [MaxLength(30)]
        public string? orig_data { get; set; }

        [MaxLength(1)]
        public string? source_tran { get; set; }

        [MaxLength(1)]
        public string? auth_tran { get; set; }

        public string? node_data_req { get; set; }
        public string? node_data_rsp { get; set; }

        [MaxLength(4)]
        public string? resp_code_rsp { get; set; }

        [MaxLength(120)]
        public string? additional_amount { get; set; }

        [MaxLength(1)]
        public string? tran_reversed { get; set; }

        [MaxLength(50)]
        public string? ip_endpoint { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? time_req { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? time_rsp { get; set; }

        // ---- Added for Queries > Transaction detail view (§7.27) ----

        [MaxLength(3)]
        public string? pos_entry_mode { get; set; }

        [MaxLength(4)]
        public string? msgtype { get; set; }

        [MaxLength(4)]
        public string? resp_code_rev { get; set; }

        [MaxLength(4)]
        public string? resp_code_adv { get; set; }

        [MaxLength(999)]
        public string? icc_data { get; set; }

        public decimal? fee_mer { get; set; }
        public decimal? fee_sub { get; set; }
    }
}
