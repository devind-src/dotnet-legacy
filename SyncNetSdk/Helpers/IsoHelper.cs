using SyncNet.IsoMessage;
using SyncNet.Message;

namespace SyncNet.Helpers
{
    public static class IsoHelper
    {
        public static Request GetSdkMsgRequest(this Iso8583 iso)
        {
            return IsoConverter.GetSdkMsgRequest(iso);
        }
        public static Response GetSdkMsgResponse(this Iso8583 iso)
        {
            return IsoConverter.GetSdkMsgResponse(iso);
        }

        public static Iso8583 GetIsoMessage(this Request req)
        {
            return IsoConverter.GetIsoMessage(req);
        }
        public static Iso8583 GetIsoMessage(this Response res)
        {
            return IsoConverter.GetIsoMessage(res);
        }
    }
}
