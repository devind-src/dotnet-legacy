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

        public static Response Inquiry(Iso8583 iso, Request req)
        {
            //get biller data
            var de = GetDataElementResponse(iso, req);

            //construct message response            
            var rsp = new Response(req);
            rsp.msgtype = "0110";
            rsp.resp_code = iso.GetField(39).MappingRC();
            rsp.resp_message = iso.GetField(39).MappingRespMessage();
            
            rsp.additional_data.Add(DE_RESPONSE, de);

            return rsp;
        }
        public static Response Payment(Iso8583 iso, Request req)
        {
            //get biller data
            var de = GetDataElementResponse(iso, req);

            //construct message response            
            var rsp = new Response(req);
            rsp.msgtype = "0210";
            rsp.resp_code = iso.GetField(39).MappingRC();
            rsp.resp_message = iso.GetField(39).MappingRespMessage();

            rsp.additional_data.Add(DE_RESPONSE, de);

            return rsp;
        }
        public static Response Advice(Iso8583 iso, Request req)
        {
            //get biller data
            var de = GetDataElementResponse(iso, req);

            //construct message response            
            var rsp = new Response(req);
            rsp.msgtype = "0230";
            rsp.resp_code = iso.GetField(39).MappingRC();
            rsp.resp_message = iso.GetField(39).MappingRespMessage();

            rsp.additional_data.Add(DE_RESPONSE, de);

            return rsp;
        }
        public static Response Reversal(Iso8583 iso, Request req)
        {
            //get biller data
            var de = GetDataElementResponse(iso, req);

            //construct message response            
            var rsp = new Response(req);
            rsp.msgtype = "0410";
            rsp.resp_code = iso.GetField(39).MappingRC();
            rsp.resp_message = iso.GetField(39).MappingRespMessage();

            rsp.additional_data.Add(DE_RESPONSE, de);

            return rsp;
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
        private static DataElementModel.Response GetDataElementResponse(Iso8583 iso, Request req)
        {
            var ret = new DataElementModel.Response();
            
            try
            {
                //get data element from request
                var de = GetDataElementRequest(req);

                //get biller data from iso
                ret.echoData = de.echoData; // echo data from request
                ret.billerData = iso.GetField(48);
                ret.respCodeHost = iso.GetField(39);
            }
            catch { }

            return ret;
        }
        #endregion
    }
}
