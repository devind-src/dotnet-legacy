using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Nodes
{
    /// <summary>NodeId/PortIn/PortOut are NOT supplied by the caller — node_id computed
    /// server-side as max(id)+1, ports auto-assigned as the next sequential value (matches
    /// legacy NodeGetNewPortIn/NodeGetNewPortOut). Range constraints on the timers/timeouts
    /// match legacy IsValid(). Key fields default to the legacy SwCryptoKey() constructor
    /// values when omitted (zero-padded placeholders) — use the Generate buttons
    /// (api/v1/key-management/*) to fill real-looking placeholder values.</summary>
    public class CreateNodeRequest
    {
        [Required, MaxLength(20)]
        public string NodeName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string AppName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string BusinessCalendar { get; set; } = string.Empty;

        [Required, MaxLength(11)]
        public string InstId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Parameter { get; set; }

        public int? TeamId { get; set; }

        [RegularExpression("^[012]$")]
        public string Category { get; set; } = "0";

        public bool AutoSignon { get; set; }

        [RegularExpression("^[012]$")]
        public string AutoReversal { get; set; } = "1";

        public bool AutoReplyReversal { get; set; }

        [Required, Range((short)3, (short)3600)]
        public short RequestTimeout { get; set; } = 30;

        [Required, Range((short)3, (short)3600)]
        public short AdviceTimeout { get; set; } = 15;

        [Range((short)0, (short)60)]
        public short KeychangeTimer { get; set; }

        [Range((short)0, (short)60)]
        public short EchoTimer { get; set; }

        [Range(0, 100)]
        public int SafLimit { get; set; } = 3;

        public bool PinTranslate { get; set; } = true;

        public bool SensitiveData { get; set; }

        public bool SaveRepeatReversal { get; set; }

        [RegularExpression("^[123]$")]
        public string KeyLength { get; set; } = "2";

        [RegularExpression("^0[0-4]$")]
        public string PinblockFormat { get; set; } = "01";

        [MaxLength(49)]
        public string? MasterKey { get; set; }

        [MaxLength(16)]
        public string? MasterKcv { get; set; }

        [MaxLength(49)]
        public string? KeyUnderLmk { get; set; }

        [MaxLength(49)]
        public string? KeyUnderZmk { get; set; }

        [MaxLength(48)]
        public string? KeyCheckValue { get; set; }
    }
}
