using System;

namespace SyncNet.Message
{
    public class Security
    {
        public enum EnumHsmCommand
        {
            GenerateKey = 0,
            GenerateKeyTerminal = 1,
            TranslateKey = 2,
            TranslatePinblock = 3,
            TranslatePinblockTerminal = 4
        }

        //data
        public string protect_sensitive_data { get; set; }
        public string track2data { get; set; }
        public string iccdata { get; set; }
        public string pindata { get; set; }
        public string miscdata { get; set; }

        //setting
        public string pin_translate { get; set; }
        public bool is_pin_change { get; set; }
        public bool is_debet_tran { get; set; }
        public bool is_translate_from_terminal { get; set; }

        //request to hsm
        public DateTime hsm_time_req { get; set; }
        public EnumHsmCommand hsm_cmd { get; set; }
        public string hsm_data { get; set; }
        public string hsm_err_code { get; set; }
    }
}
