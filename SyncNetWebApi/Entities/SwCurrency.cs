using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_currencies" (jamak) — Configuration &gt; Base &gt; Currency.
    /// currency_code is char(3) NOT NULL (PK). alpha_code is char(3) nullable. name is
    /// varchar(20) NOT NULL. nr_decimals is integer NOT NULL. rate is double precision NOT NULL
    /// — column widths verified live via information_schema.</summary>
    [Table("sw_currencies", Schema = "public")]
    public class SwCurrency
    {
        [Key]
        public string currency_code { get; set; } = string.Empty;
        public string? alpha_code { get; set; }
        public string name { get; set; } = string.Empty;
        public int nr_decimals { get; set; }
        public double rate { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
