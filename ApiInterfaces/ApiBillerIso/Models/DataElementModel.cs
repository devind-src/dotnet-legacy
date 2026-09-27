namespace ApiBiller.Models
{
    public class DataElementModel
    {
        internal class Request
        {
            public string billerData { get; set; }
            public string echoData { get; set; }
        }
        internal class Response : Request
        {
            public string respCodeHost { get; set; }
        }
    }
}
