namespace SyncNet.Models.HsmServiceModel
{
    public class XTranslatePinblockTerminalRequest
    {
        public string terminal_id { get; set; }
        public string node_dest { get; set; }
        public string source_pinblock { get; set; }
        public string account_number { get; set; }
    }
}
