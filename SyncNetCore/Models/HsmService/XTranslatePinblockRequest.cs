namespace SyncNet.Models.HsmService
{
    public class XTranslatePinblockRequest
    {
        public string node_source { get; set; }
        public string node_dest { get; set; }
        public string source_pinblock { get; set; }
        public string account_number { get; set; }
    }
}
