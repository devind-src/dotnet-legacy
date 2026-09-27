using SyncNet.Library;
using SyncNet.Models.HsmDeviceModel;
using System;

namespace SyncNet.Common
{
    public class HsmCommand
    {
        public const string GenerateZPK = "IA";
        public const string GenerateTPK = "HC";
        public const string GenerateKCV = "BU";

        public const string TranslateKeyToLmk = "FA";
        public const string TranslatePinblock = "CC";
        public const string TranslatePinblockFromTPKToZPK = "CA";

        public static string GetHeader()
        {
            string dtnow = DateTime.Now.ToString("yyyyMMddmmssfff");
            int len = AppConfig.HsmLenHeader;

            return dtnow.Substring(dtnow.Length - len, len);
        }

        public static HsmResponse GetResponse(string response)
        {
            HsmResponse ret = new HsmResponse();

            try
            {
                int lenHeader = AppConfig.HsmLenHeader;
                int lenData = response.Length - lenHeader - 4;

                NbStrUtil str = new NbStrUtil(response);

                ret.message_header = str.getString(lenHeader);
                ret.command_response = str.getString(2);
                ret.error_code = str.getString(2);
                ret.data = str.getString(lenData);
            }
            catch { }

            return ret;
        }
    }
}
