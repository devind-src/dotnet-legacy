namespace ApiBiller.Models
{
    public class TransactionModel
    {
        public class Request
        {
            public string productId { get; set; }
            public string customerId { get; set; }
            public long amount { get; set; }
            public string refnum { get; set; }
        }
        public class Response : Request
        {
            public string respCode { get; set; }
            public string respMessage { get; set; }
            public BillerData billerData { get; set; }

            public class BillerData
            {
                public string transactionId { get; set; }
                public string customerName { get; set; }
                public int adminFee { get; set; }
                public string billPeriod { get; set; }
                public string miscData { get; set; }
            }
        }
    }
}
