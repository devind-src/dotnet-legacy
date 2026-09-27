namespace ApiChannel.Models.Channel
{
    public class DataElementModel
    {
        public class Request
        {
            public string privateData { get; set; }
        }

        public class Response
        {
            public string respCodeHost { get; set; }
            public string refnumBiller { get; set; }
            public string customerName { get; set; }
            public string miscData { get; set; }
            public int adminFee { get; set; }
        }
    }
}
