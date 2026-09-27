using SyncNet.Constants;
using SyncNet.Library;
using System.Collections.Generic;

namespace SyncNet.Message
{
    public class Response
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
        public string pos_entry_mode { get; set; }
        public string refnum { get; set; }
        public string receiving_inst_id { get; set; }
        public string from_acc_number { get; set; }
        public string to_acc_number { get; set; }
        public string additional_amount { get; set; }
        public string resp_code { get; set; }
        public string resp_message { get; set; }
        public string authorized_by { get; set; }

        //reply orig msg from request
        public object echo_data { get; set; }

        //bypass message without save to database
        public object temp_data { get; set; }

        //tran type + datetime + trace -> key utk msg reversal & advice
        public string original_data { get; set; }

        public Dictionary<string, object> additional_data { get; set; }
        public Fees fee_data { get; set; }
        public Security security { get; set; }
        public PrivateData private_data { get; set; }
        public VirtualAccount virtual_account { get; set; }

        public Response()
        {
            fee_data = new Fees();
            security = new Security();
            private_data = new PrivateData();
            virtual_account = new VirtualAccount();
            additional_data = new Dictionary<string, object>();

            authorized_by = AuthTran.EXTERNAL;
        }

        public Response(Message.Request req)
        {
            msgtype = NbMessage.GetMsgTypeResp(req.msgtype);

            fee_data = req.fee_data;
            security = req.security;
            private_data = req.private_data;
            virtual_account = req.virtual_account;

            pan = req.pan;
            tran_type = req.tran_type;
            tran_type_ext = req.tran_type_ext;
            from_acc_type = req.from_acc_type;
            to_acc_type = req.from_acc_type;
            currency = req.currency;
            amount_tran = req.amount_tran;
            trace_number = req.trace_number;
            datetime_tran = req.datetime_tran;
            date_settle = req.date_settle;
            merchant_type = req.merchant_type;
            merchant_id = req.merchant_id;
            terminal_id = req.terminal_id;
            acq_inst_id = req.acq_inst_id;
            fwd_inst_id = req.fwd_inst_id;
            pos_entry_mode = req.pos_entry_mode;
            refnum = req.refnum;
            receiving_inst_id = req.receiving_inst_id;
            from_acc_number = req.from_acc_number;
            to_acc_number = req.to_acc_number;
            echo_data = req.echo_data;
            original_data = req.original_data;
            additional_data = req.additional_data;

            authorized_by = AuthTran.EXTERNAL;
        }
    }
}
