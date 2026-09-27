using ApiChannel.Models.Channel;
using Newtonsoft.Json;
using SyncNet.Helpers;

namespace ApiChannel.Helpers
{
    static class MessageHelper
    {
        public static string GetMerchantId(this string json)
        {
            string res = string.Empty;

            try
            {
                //extract essensial field
                var req = JsonConvert.DeserializeObject<PaymentModel.Request>(json);
                res = req.merchantId;
            }
            catch { }

            return res;
        }
        public static string GetBalance(this string value)
        {
            if (string.IsNullOrEmpty(value) == true) return "0";

            return value.ToBigNumber().ToString();
        }
        public static string GetTranType(this string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Length != 6) return value;

            return value.Substring(0, 2);
        }
        public static string GetFromAccType(this string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Length != 6) return value;

            return value.Substring(2, 2);
        }
        public static string GetToAccType(this string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Length != 6) return value;

            return value.Substring(4, 2);
        }
        public static string MapRC(this string value)
        {
            string result = value;

            //map response code
            switch (value)
            {
                default:
                    result = value;
                    break;
            }

            return result;
        }
    }
}
