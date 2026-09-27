using ApiBiller.Common;
using ApiBiller.Helpers;
using ApiBiller.Models;
using Newtonsoft.Json;
using SyncNet.IsoMessage;
using SyncNet.Library;
using SyncNet.Message;

namespace ApiBiller.Message
{
    class ToInternal
    {
        private static string DE_REQUEST = "request";
        private static string DE_RESPONSE = "response";

        public static Response Inquiry(TransactionModel.Response res, Request req)
        {
            //construct message response            
            var result = new Response(req)
            {
                resp_code = res.respCode,
                resp_message = res.respMessage
            };

            //get biller data
            var de = GetDataElementResponse(res, req);

            result.additional_data.Add(DE_RESPONSE, de);

            return result;
        }
        public static Response Payment(TransactionModel.Response res, Request req)
        {
            //construct message response            
            var result = new Response(req)
            {
                resp_code = res.respCode,
                resp_message = res.respMessage
            };

            //get biller data
            var de = GetDataElementResponse(res, req);

            result.additional_data.Add(DE_RESPONSE, de);

            return result;
        }
        public static Response Advice(TransactionModel.Response res, Request req)
        {
            //construct message response            
            var result = new Response(req)
            {
                resp_code = res.respCode,
                resp_message = res.respMessage
            };

            //get biller data
            var de = GetDataElementResponse(res, req);

            result.additional_data.Add(DE_RESPONSE, de);

            return result;
        }
        public static Response Reversal(TransactionModel.Response res, Request req)
        {
            //construct message response            
            var result = new Response(req)
            {
                resp_code = res.respCode,
                resp_message = res.respMessage
            };

            //get biller data
            var de = GetDataElementResponse(res, req);

            result.additional_data.Add(DE_RESPONSE, de);

            return result;
        }


        #region Private Method
        private static DataElementModel.Request GetDataElementRequest(Request req)
        {
            var retval = new DataElementModel.Request();

            try
            {
                if (req.additional_data.TryGetValue(DE_REQUEST, out object value) == true)
                {
                    string json = JsonConvert.SerializeObject(value);
                    retval = JsonConvert.DeserializeObject<DataElementModel.Request>(json);
                }
            }
            catch { }

            return retval;
        }
        private static DataElementModel.Response GetDataElementResponse(TransactionModel.Response res, Request req)
        {
            var ret = new DataElementModel.Response();
            
            try
            {
                //get data element from request
                var de = GetDataElementRequest(req);

                //get biller data
                ret.respCodeHost = res.respCode;
                ret.refnumBiller = res.billerData.transactionId;
                ret.customerName = res.billerData.customerName;
                ret.miscData = res.billerData.miscData;
            }
            catch { }

            return ret;
        }
        #endregion
    }
}
