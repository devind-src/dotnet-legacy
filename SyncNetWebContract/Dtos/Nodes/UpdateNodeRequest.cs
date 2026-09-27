using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Nodes
{
    /// <summary>NodeName/AppName/PortIn/PortOut are immutable after create — matches legacy
    /// UI, which renders NodeName/AppName readonly on edit and never lets ports be edited at
    /// all.</summary>
    public class UpdateNodeRequest
    {
        [Required, MaxLength(20)]
        public string BusinessCalendar { get; set; } = string.Empty;

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
