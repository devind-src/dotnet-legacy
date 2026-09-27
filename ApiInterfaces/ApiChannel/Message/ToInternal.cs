using ApiChannel.Helpers;
using ApiChannel.Models.Channel;
using Newtonsoft.Json;
using SyncNet.Constants;
using SyncNet.Helpers;
using SyncNet.Message;

namespace ApiChannel.Message
{
    internal class ToInternal
    {
        private static string DE_REQUEST = "request";

        public static Request Inquiry(string json)
        {
            var req = JsonConvert.DeserializeObject<PaymentModel.Request>(json);

            var m = new Request();
            m.msgtype = "0100";
            m.tran_type = TranType.INQUIRY;
            m.tran_type_ext = "38";
            m.from_acc_type = "00";
            m.to_acc_type = "00";
            m.amount_tran = req.amount.ToBigNumber();
            m.trace_number = req.traceNumber;
            m.datetime_tran = req.dateTime;
            m.merchant_id = req.merchantId;
            m.terminal_id = req.terminalId;
            m.refnum = req.referenceNumber;
            m.receiving_inst_id = req.productId;
            m.to_acc_number = req.customerId;

            var x = new DataElementModel.Request();
            x.privateData = "test private data";

            m.additional_data.Add(DE_REQUEST, x);

            return m;
        }
        public static Request Payment(string json)
        {
            var req = JsonConvert.DeserializeObject<PaymentModel.Request>(json);

            var m = new Request();
            m.msgtype = "0200";
            m.tran_type = TranType.PAYMENT;
            m.tran_type_ext = "50";
            m.from_acc_type = "00";
            m.to_acc_type = "00";
            m.amount_tran = req.amount.ToBigNumber();
            m.trace_number = req.traceNumber;
            m.datetime_tran = req.dateTime;
            m.merchant_id = req.merchantId;
            m.terminal_id = req.terminalId;
            m.refnum = req.referenceNumber;
            m.receiving_inst_id = req.productId;
            m.to_acc_number = req.customerId;

            var x = new DataElementModel.Request();
            x.privateData = "test private data";

            m.additional_data.Add(DE_REQUEST, x);

            return m;
        }
        public static Request Advice(string json)
        {
            var req = JsonConvert.DeserializeObject<PaymentModel.Request>(json);

            var m = new Request();
            m.msgtype = "0220";
            m.tran_type = TranType.ADVICE;
            m.tran_type_ext = "50";
            m.from_acc_type = "00";
            m.to_acc_type = "00";
            m.amount_tran = req.amount.ToBigNumber();
            m.trace_number = req.traceNumber;
            m.datetime_tran = req.dateTime;
            m.merchant_id = req.merchantId;
            m.terminal_id = req.terminalId;
            m.refnum = req.referenceNumber;
            m.receiving_inst_id = req.productId;
            m.to_acc_number = req.customerId;
            m.original_data = TranType.PAYMENT + req.originalData;

            var x = new DataElementModel.Request();
            x.privateData = "test private data";

            m.additional_data.Add(DE_REQUEST, x);

            return m;
        }
        public static Request Reversal(string json)
        {
            var req = JsonConvert.DeserializeObject<PaymentModel.Request>(json);

            var m = new Request();
            m.msgtype = "0400";
            m.tran_type = TranType.REVERSAL;
            m.tran_type_ext = "50";
            m.from_acc_type = "00";
            m.to_acc_type = "00";
            m.amount_tran = req.amount.ToBigNumber();
            m.trace_number = req.traceNumber;
            m.datetime_tran = req.dateTime;
            m.merchant_id = req.merchantId;
            m.terminal_id = req.terminalId;
            m.refnum = req.referenceNumber;
            m.receiving_inst_id = req.productId;
            m.to_acc_number = req.customerId;
            m.original_data = TranType.PAYMENT + req.originalData;

            var x = new DataElementModel.Request();
            x.privateData = "test private data";

            m.additional_data.Add(DE_REQUEST, x);

            return m;
        }
    }
}
