using ApiChannel.Helpers;
using ApiChannel.Library;
using ApiChannel.Models.Channel;
using Newtonsoft.Json;
using SyncNet.Message;
using System;

namespace ApiChannel.Message
{
    internal class ToRemote
    {
        private static string DE_RESPONSE = "response";

        public static string Echo(string reqJson)
        {
            var req = JsonConvert.DeserializeObject<EchoModel.Request>(reqJson);

            //construct response
            var m = new EchoModel.Response(req);
            m.data.traceNumber = req.traceNumber;
            m.data.dateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            m.data.merchantId = req.merchantId;
            m.data.terminalId = req.terminalId;
            m.data.appVersion = MyApp.VERSION;

            var b = RespCode.GetObject(RespCode.SUCCESS);
            m.responseStatus = b.responseStatus;
            m.responseMessage = b.responseMessage;

            return JsonConvert.SerializeObject(m);
        }

        public static string Inquiry(string reqJson, Response rsp)
        {
            var req = JsonConvert.DeserializeObject<PaymentModel.Request>(reqJson);

            //get additional data response
            var de = GetDataElementResponse(rsp);

            //construct response
            var m = new PaymentModel.Response(req);
            m.responseStatus = rsp.resp_code.MapRC();
            m.responseMessage = rsp.resp_message;

            m.data.transactionInfo.balance = rsp.additional_amount.GetBalance();
            m.data.transactionInfo.fee = rsp.fee_data.total_fee.ToString();
            m.data.transactionInfo.customerName = de.customerName;
            m.data.transactionInfo.miscData = de.miscData;

            //trx rejected data null
            if (m.responseStatus != "00") m.data = null;

            return JsonConvert.SerializeObject(m);
        }
        public static string Payment(string reqJson, Response rsp)
        {
            var req = JsonConvert.DeserializeObject<PaymentModel.Request>(reqJson);

            //get additional data response
            var de = GetDataElementResponse(rsp);

            //construct response
            var m = new PaymentModel.Response(req);
            m.responseStatus = rsp.resp_code.MapRC();
            m.responseMessage = rsp.resp_message;

            m.data.transactionInfo.balance = rsp.additional_amount.GetBalance();
            m.data.transactionInfo.fee = rsp.fee_data.total_fee.ToString();
            m.data.transactionInfo.customerName = de.customerName;
            m.data.transactionInfo.miscData = de.miscData;
            m.data.transactionInfo.referenceNumber = de.refnumBiller; //replace with refnum biller

            //trx rejected data null
            if (m.responseStatus != "00" && m.responseStatus != "68") m.data = null;

            //for struct
            if (m.responseStatus == "68")
            {
                m.responseMessage = "SUSPECT";
            }

            return JsonConvert.SerializeObject(m);
        }
        public static string Advice(string reqJson, Response rsp)
        {
            var req = JsonConvert.DeserializeObject<PaymentModel.Request>(reqJson);

            //get additional data response
            var de = GetDataElementResponse(rsp);

            //construct response
            var m = new PaymentModel.Response(req);
            m.responseStatus = rsp.resp_code.MapRC();
            m.responseMessage = rsp.resp_message;

            m.data.transactionInfo.balance = rsp.additional_amount.GetBalance();
            m.data.transactionInfo.fee = rsp.fee_data.total_fee.ToString();
            m.data.transactionInfo.customerName = de.customerName;
            m.data.transactionInfo.miscData = de.miscData;
            m.data.transactionInfo.referenceNumber = de.refnumBiller; //replace with refnum biller

            //trx rejected data null
            if (m.responseStatus != "00" && m.responseStatus != "68") m.data = null;

            //for struct
            if (m.responseStatus == "68")
            {
                m.responseMessage = "SUSPECT";
            }

            return JsonConvert.SerializeObject(m);
        }
        public static string Reversal(string reqJson, Response rsp)
        {
            var req = JsonConvert.DeserializeObject<PaymentModel.Request>(reqJson);

            //get additional data response
            var de = GetDataElementResponse(rsp);

            //construct response
            var m = new PaymentModel.Response(req);
            m.responseStatus = rsp.resp_code.MapRC();
            m.responseMessage = rsp.resp_message;

            m.data.transactionInfo.balance = rsp.additional_amount.GetBalance();
            m.data.transactionInfo.fee = rsp.fee_data.total_fee.ToString();
            m.data.transactionInfo.customerName = de.customerName;
            m.data.transactionInfo.miscData = de.miscData;
            m.data.transactionInfo.referenceNumber = de.refnumBiller; //replace with refnum biller

            //trx rejected data null
            if (m.responseStatus != "00" && m.responseStatus != "68") m.data = null;

            //for struct
            if (m.responseStatus == "68")
            {
                m.responseMessage = "SUSPECT";
            }

            return JsonConvert.SerializeObject(m);
        }

        private static DataElementModel.Response GetDataElementResponse(Response rsp)
        {
            var ret = new DataElementModel.Response();

            try
            {
                if (rsp.additional_data.TryGetValue(DE_RESPONSE, out object obj) == true)
                {
                    string json = JsonConvert.SerializeObject(obj);
                    ret = JsonConvert.DeserializeObject<DataElementModel.Response>(json);
                }
            }
            catch { }

            return ret;
        }
    }
}
