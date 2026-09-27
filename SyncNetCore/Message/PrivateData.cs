namespace SyncNet.Message
{
    public class PrivateData
    {
        public string source_node { get; set; }
        public string sink_node { get; set; }
        public string ip_source { get; set; }
        public string ip_external { get; set; }
        public string connection_name { get; set; }

        //0-none,1-auto rev,2-auto adv
        public string mode_timeout { get; set; }
        public string auto_reply_rev { get; set; }
        public string save_repeat_reversal { get; set; }
        public bool is_req_internal { get; set; }

        //retry send when timeout
        public int retry_send { get; set; }
        public int max_retry_send { get; set; }

        //business calendar
        public int enable_closing { get; set; }
        public string closing_time_start { get; set; }
        public string closing_time_end { get; set; }
        public string calendar_name { get; set; }

        public PrivateData()
        {
            mode_timeout = "0";//0-none,1-auto rev,2-auto adv
            retry_send = 0;
        }
    }
}
