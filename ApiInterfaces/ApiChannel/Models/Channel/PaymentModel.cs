namespace ApiChannel.Models.Channel
{
    internal class PaymentModel
    {
        public class Request
        {
            public string amount { get; set; }
            public string dateTime { get; set; }
            public string traceNumber { get; set; }
            public string referenceNumber { get; set; }
            public string merchantId { get; set; }
            public string terminalId { get; set; }
            public string productId { get; set; }
            public string customerId { get; set; }
            public string originalData { get; set; } //digunakan untuk cek status
        }

        public class Response
        {
            public string responseStatus { get; set; }
            public string responseMessage { get; set; }
            public ResponseData data { get; set; }

            public Response(Request req)
            {
                if (req == null) return;

                data = new ResponseData();
                data.terminalInfo.terminalId = req.terminalId;
                data.terminalInfo.merchantId = req.merchantId;

                data.transactionInfo.dateTime = req.dateTime;
                data.transactionInfo.referenceNumber = req.referenceNumber;
                data.transactionInfo.traceNumber = req.traceNumber;
                data.transactionInfo.amount = req.amount.ToString();
                data.transactionInfo.customerId = req.customerId;               
            }

            public class ResponseData
            {
                public TerminalInfo terminalInfo { get; set; }
                public TransactionInfo transactionInfo { get; set; }

                public ResponseData()
                {
                    terminalInfo = new TerminalInfo();
                    transactionInfo = new TransactionInfo();
                }
            }
        }
    }
}
