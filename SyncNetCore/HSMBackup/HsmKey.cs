namespace SWTCoreLab.HSM
{
    class HsmKey
    {
        public string NodeName { get; set; }
        public string MasterKey { get; set; }
        public string KeyUnderLMK { get; set; }
        public string KeyUnderZMK { get; set; }
        public string KeyCheckValue { get; set; }
        public string PinBlockFormat { get; set; }
    }
    class TerminalKey
    {
        public string merchant_id { get; set; }
        public string serialNumber { get; set; }
        public string phoneNumber { get; set; }
        public string tmk_under_lmk { get; set; }
        public string tpk_under_lmk { get; set; }
        public string status { get; set; }
    }
}
