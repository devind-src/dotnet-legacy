using System.Collections.Generic;

namespace SyncNet.Message
{
    public class Request
    {
        public string pan { get; set; }
        public string msgtype { get; set; }
        public string tran_type { get; set; }
        public string tran_type_ext { get; set; }
        public string from_acc_type { get; set; }
        public string to_acc_type { get; set; }
        public string currency { get; set; }
        public decimal amount_tran { get; set; }
        public string trace_number { get; set; }
        public string datetime_tran { get; set; }
        public string date_settle { get; set; }
        public string merchant_type { get; set; }
        public string merchant_id { get; set; }
        public string terminal_id { get; set; }
        public string acq_inst_id { get; set; }
        public string fwd_inst_id { get; set; }
        public string refnum { get; set; }
        public string pos_entry_mode { get; set; }
        public string receiving_inst_id { get; set; }
        public string from_acc_number { get; set; }
        public string to_acc_number { get; set; }

        //reply orig msg from request
        public object echo_data { get; set; }

        //by pass without save to db
        public object temp_data { get; set; }

        //datetime + trace -> key utk msg reversal & advice
        public string original_data { get; set; }

        public Dictionary<string, object> additional_data { get; set; }
        public Fees fee_data { get; set; }
        public Security security { get; set; }
        public PrivateData private_data { get; set; }
        public VirtualAccount virtual_account { get; set; }

        public Request()
        {
            additional_data = new Dictionary<string, object>();
            virtual_account = new VirtualAccount();
            private_data = new PrivateData();
            security = new Security();
            fee_data = new Fees();
        }
    }
}
