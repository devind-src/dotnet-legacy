namespace SyncNet.Message
{
    public class PrivateData
    {
        //** field yg di perlukan saja yang ditaruh di SDK **//
        public string sink_node { get; set; }

        public string ip_external { get; set; }
        public string connection_name { get; set; }

        //retry send when timeout
        public int retry_send { get; set; }
    }
}
