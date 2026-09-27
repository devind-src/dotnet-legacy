using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "va_trans_request" — backs Virtual Account &gt; Topup, Adjustment and
    /// Approval (three different filtered views over the same table, same as legacy's three
    /// separate Index pages). tran_nr (int) was manually computed as max(tran_nr)+1 (legacy
    /// quirk); converted to a real Postgres IDENTITY column — DB now assigns it on insert.
    /// tran_type: "80"=Topup, "90"=Adjustment Credit, "91"=Adjustment Debet.
    /// debet_credit: "D"/"C". status: "0"=Request/Pending, "1"=Approved, "2"=Rejected.
    /// balance here is computed in memory during Approval but legacy's raw UPDATE never
    /// actually persists it (verified against real approved rows — always 0) — replicated as
    /// a dead-write field, not "fixed", per project convention of following legacy as-is.</summary>
    [Table("va_trans_request", Schema = "public")]
    public class VaTransRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int tran_nr { get; set; }

        [MaxLength(30)]
        public string? acc_nr { get; set; }

        [MaxLength(2)]
        public string? tran_type { get; set; }

        [MaxLength(1)]
        public string? debet_credit { get; set; }

        [MaxLength(12)]
        public string? trace_nr { get; set; }

        [MaxLength(30)]
        public string? reff_nr { get; set; }

        // Physical column is "timestamp without time zone" (verified live) — annotate
        // explicitly, otherwise Npgsql/EF defaults unannotated DateTime to timestamptz, which
        // bit Phase P6 (sw_fees_promo.date_start/date_end) the moment the column is ever used
        // in a query predicate. Not filtered on today, but cheap insurance.
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? request_date { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? approval_date { get; set; }

        [MaxLength(50)]
        public string? description { get; set; }

        public long? amount { get; set; }
        public long? balance { get; set; }

        [MaxLength(30)]
        public string? usr_name { get; set; }

        [MaxLength(30)]
        public string? spv_name { get; set; }

        [MaxLength(1)]
        public string? status { get; set; }
    }
}
