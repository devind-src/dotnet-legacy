using ApiBiller.Common;
using ApiBiller.Helpers;
using ApiBiller.Models;
using Newtonsoft.Json;
using SyncNet.IsoMessage;
using SyncNet.Library;
using SyncNet.Message;
using System;

namespace ApiBiller.Message
{
    class ToRemote
    {
        private static string DE_REQUEST = "request";

        public static Iso8583 NtwrkMgmt(string Code)
        {
            string dtnow = DateTime.Now.ToString("yyyyMMddHHmmss");

            var iso = new Iso8583(new IsoTemplate());
            iso.MsgType = "0800";//message type
            iso.PutField(7, dtnow.AdjustTranDateTimeGMT()); //date time trans
            iso.PutField(11, NbRandom.GenerateNumber(6)); //trace number
            iso.PutField(32, ApiConfig.AcqInstId); //acq inst id
            iso.PutField(70, Code); //network management code

            return iso;
        }

        public static Iso8583 Inquiry(Request req)
        {
            string pcode = GetProcessingCode(req);

            //data from channel
            var de = GetDataElementRequest(req);

            var iso = new Iso8583(new IsoTemplate());
            iso.MsgType = "0100";
            iso.PutField(3, pcode);
            iso.PutField(4, req.amount_tran.AddDecPoint());
            iso.PutField(7, req.datetime_tran.AdjustTranDateTimeGMT());
            iso.PutField(11, req.trace_number.AdjustTrace());
            iso.PutField(12, req.datetime_tran.AdjustTime());
            iso.PutField(13, req.datetime_tran.AdjustDate());
            iso.PutField(18, req.merchant_type);
            iso.PutField(32, ApiConfig.AcqInstId); //get from config
            iso.PutField(33, ApiConfig.FwdInstId); //get from config
            iso.PutField(37, req.refnum);
            iso.PutField(41, req.terminal_id.AdjustSpace(8));
            iso.PutField(42, req.merchant_id.AdjustSpace(15));
            iso.PutField(48, de.billerData ?? ""); //biller data;
            iso.PutField(49, req.currency ?? "360");
            iso.PutField(100, req.receiving_inst_id); //product id
            iso.PutField(103, req.to_acc_number); //customer id

            return iso;
        }

        public static Iso8583 Payment(Request req)
        {
            string pcode = GetProcessingCode(req);

            //data from terminal
            var de = GetDataElementRequest(req);

            var iso = new Iso8583(new IsoTemplate());
            iso.MsgType = "0200";
            iso.PutField(3, pcode);
            iso.PutField(4, req.amount_tran.AddDecPoint());
            iso.PutField(7, req.datetime_tran.AdjustTranDateTimeGMT());
            iso.PutField(11, req.trace_number.AdjustTrace());
            iso.PutField(12, req.datetime_tran.AdjustTime());
            iso.PutField(13, req.datetime_tran.AdjustDate());
            iso.PutField(18, req.merchant_type);
            iso.PutField(32, ApiConfig.AcqInstId); //get from config
            iso.PutField(33, ApiConfig.FwdInstId); //get from config
            iso.PutField(37, req.refnum);
            iso.PutField(41, req.terminal_id.AdjustSpace(8));
            iso.PutField(42, req.merchant_id.AdjustSpace(15));
            iso.PutField(48, de.billerData ?? ""); //biller data;
            iso.PutField(49, req.currency ?? "360");
            iso.PutField(100, req.receiving_inst_id); //product id
            iso.PutField(103, req.to_acc_number); //customer id

            return iso;
        }

        public static Iso8583 Advice(Request req)
        {
            string pcode = GetProcessingCode(req);

            //data from terminal
            var de = GetDataElementRequest(req);

            var iso = new Iso8583(new IsoTemplate());
            iso.MsgType = "0220";
            iso.PutField(3, pcode);
            iso.PutField(4, req.amount_tran.AddDecPoint());
            iso.PutField(7, req.datetime_tran.AdjustTranDateTimeGMT());
            iso.PutField(11, req.trace_number.AdjustTrace());
            iso.PutField(12, req.datetime_tran.AdjustTime());
            iso.PutField(13, req.datetime_tran.AdjustDate());
            iso.PutField(18, req.merchant_type);
            iso.PutField(32, ApiConfig.AcqInstId); //get from config
            iso.PutField(33, ApiConfig.FwdInstId); //get from config
            iso.PutField(37, req.refnum);
            iso.PutField(41, req.terminal_id.AdjustSpace(8));
            iso.PutField(42, req.merchant_id.AdjustSpace(15));
            iso.PutField(48, de.billerData ?? ""); //biller data;
            iso.PutField(49, req.currency ?? "360");
            iso.PutField(100, req.receiving_inst_id); //product id
            iso.PutField(103, req.to_acc_number); //customer id

            return iso;
        }

        public static Iso8583 Reversal(Request req)
        {
            string pcode = GetProcessingCode(req);

            //data from terminal
            var de = GetDataElementRequest(req);

            var iso = new Iso8583(new IsoTemplate());
            iso.MsgType = "0400";
            iso.PutField(3, pcode);
            iso.PutField(4, req.amount_tran.AddDecPoint());
            iso.PutField(7, req.datetime_tran.AdjustTranDateTimeGMT());
            iso.PutField(11, req.trace_number.AdjustTrace());
            iso.PutField(12, req.datetime_tran.AdjustTime());
            iso.PutField(13, req.datetime_tran.AdjustDate());
            iso.PutField(18, req.merchant_type);
            iso.PutField(32, ApiConfig.AcqInstId); //get from config
            iso.PutField(33, ApiConfig.FwdInstId); //get from config
            iso.PutField(37, req.refnum);
            iso.PutField(41, req.terminal_id.AdjustSpace(8));
            iso.PutField(42, req.merchant_id.AdjustSpace(15));
            iso.PutField(48, de.billerData ?? ""); //biller data;
            iso.PutField(49, req.currency ?? "360");
            iso.PutField(90, req.original_data.AdjustOriginalData()); //original data
            iso.PutField(100, req.receiving_inst_id); //product id
            iso.PutField(103, req.to_acc_number); //customer id

            return iso;
        }

        #region Private Method
        private static DataElementModel.Request GetDataElementRequest(Request req)
        {
            var retval = new DataElementModel.Request();

            try
            {
                if (req.additional_data.TryGetValue(DE_REQUEST, out object value) == true)
                {
                    retval = JsonConvert.DeserializeObject<DataElementModel.Request>(value.ToString());
                }
            }
            catch { }

            return retval;
        }

        private static string GetProcessingCode(Request req)
        {
            return req.tran_type_ext + req.from_acc_type + req.to_acc_type;
        }
        #endregion
    }
}
