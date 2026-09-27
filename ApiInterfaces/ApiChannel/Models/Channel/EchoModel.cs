namespace ApiChannel.Models.Channel
{
    internal class EchoModel
    {
        public class Request
        {
            public string dateTime { get; set; }
            public string traceNumber { get; set; }
            public string merchantId { get; set; }
            public string terminalId { get; set; }
            public string networkMgmtCode { get; set; }
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
                data.dateTime = req.dateTime;
                data.traceNumber = req.traceNumber;
                data.merchantId = req.merchantId;
                data.terminalId = req.terminalId;
            }

            public class ResponseData
            {
                public string dateTime { get; set; }
                public string traceNumber { get; set; }
                public string merchantId { get; set; }
                public string terminalId { get; set; }
                public string appVersion { get; set; }
            }
        }
    }
}
