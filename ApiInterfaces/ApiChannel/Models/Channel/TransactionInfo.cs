namespace ApiChannel.Models.Channel
{
    internal class TransactionInfo
    {
        public string dateTime { get; set; }
        public string referenceNumber { get; set; }
        public string traceNumber { get; set; }
        public string amount { get; set; }
        public string balance { get; set; }
        public string fee { get; set; }
        public string customerId { get; set; }
        public string customerName { get; set; }
        public string miscData { get; set; }
    }
}
