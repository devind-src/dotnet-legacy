namespace SyncNet.Message
{
    public class Security
    {
        public enum EnumHsmCommand
        {
            //GenerateKey = 0,
            //GenerateKeyTerminal = 1,
            //TranslateKey = 2,
            TranslatePinblock = 3,
            TranslatePinblockTerminal = 4
        }

        public string track2data { get; set; }
        public string iccdata { get; set; }
        public string pindata { get; set; }
        public string miscdata { get; set; }
        public EnumHsmCommand hsm_cmd { get; set; }

        public bool is_pin_change { get; set; }
        public bool is_debet_tran { get; set; }

        public Security()
        {
            //default
            is_debet_tran = true;
        }
    }
}
