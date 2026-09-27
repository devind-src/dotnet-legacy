using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_crypto_hsm" — HSM &gt; Device. hsm_name is varchar(25), manual PK
    /// (no identity concern — string key). Real columns not mapped here because they are
    /// dead in every form this project has ever shipped (legacy included): comm_port/
    /// comm_setup (Protocol "COMM" was never implemented — legacy showed "Protocol is not
    /// supported" instead of inputs, and the option itself is dropped from this app's dropdown
    /// per product decision), em_host_port/em_console_port (Protocol "Emulator" never exposed
    /// them either), max_conn (not even present in the legacy C# entity), and status (product
    /// decision: HSM Device/Service have no Active toggle, unlike most other modules).
    /// <br/><br/>
    /// protocol: "1"=TCP/IP, "2"=Emulator (legacy's "0"=COMM removed from this app's UI).
    /// Neither this field nor <c>priority</c> drives any routing/load-balancing logic — HSM
    /// math (Services/Hsm/HsmCryptoProvider) always runs the local Emulator directly regardless
    /// of what's configured here, same as legacy (protocol was never wired to real behavior
    /// there either). Real TCP/IP network HSM integration, and multi-device load balancing for
    /// it, are explicitly out of scope: this dashboard only ever does generate/encrypt, not the
    /// transaction-time key translate that would need a live HSM connection.</summary>
    [Table("sw_crypto_hsm", Schema = "public")]
    public class SwCryptoHsm
    {
        [Key]
        [MaxLength(25)]
        public string hsm_name { get; set; } = string.Empty;

        public short? priority { get; set; }

        [MaxLength(1)]
        public string? protocol { get; set; }

        [MaxLength(1)]
        public string? use_scheme { get; set; }

        public short? message_header { get; set; }

        [MaxLength(15)]
        public string? remote_ip { get; set; }

        [MaxLength(5)]
        public string? remote_port { get; set; }

        [MaxLength(30)]
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        [MaxLength(30)]
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
