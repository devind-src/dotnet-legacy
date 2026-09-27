using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_product_transfer" — menu "Product &gt; Master &gt; Transfer" (`/product/master/transfer`).
    /// `id` (bigint) **is** `GENERATED ALWAYS AS IDENTITY` (verified live) — unlike most other
    /// manual-PK tables in this project, do not compute it manually. `name` holds a Node's
    /// `node_name` (free text picked from a Node-Biller dropdown, not a real FK), immutable
    /// after create (legacy renders it readonly on edit). Widths verified live: name varchar(50),
    /// bank_code varchar(6), bank_name varchar(50).</summary>
    [Table("sw_product_transfer", Schema = "public")]
    public class SwProductTransfer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long id { get; set; }
        public string? name { get; set; }
        public string? bank_code { get; set; }
        public string? bank_name { get; set; }
    }
}
