using System;

namespace SyncNet.Library
{
    public class NbMessage
    {
        public static string GetMsgTypeResp(string MsgTypeReq)
        {
            if (string.IsNullOrEmpty(MsgTypeReq)) return "";

            string MsgTypeResp;

            if (MsgTypeReq.Length == 4)
            {
                switch (MsgTypeReq.Substring(2, 1))
                {
                    case "0": //Request
                        MsgTypeResp = MsgTypeReq.Substring(0, 2) + "1" + MsgTypeReq.Substring(MsgTypeReq.Length - 1, 1);

                        if (MsgTypeResp.Substring(MsgTypeResp.Length - 1, 1) == "1")
                        {
                            MsgTypeResp = MsgTypeResp.Substring(0, 3) + "0";
                        }
                        break;
                    case "2": //Advice
                        MsgTypeResp = MsgTypeReq.Substring(0, 2) + "3" + MsgTypeReq.Substring(MsgTypeReq.Length - 1, 1);

                        if (MsgTypeResp.Substring(MsgTypeResp.Length - 1, 1) == "1")
                        {
                            MsgTypeResp = MsgTypeResp.Substring(0, 3) + "0";
                        }
                        break;
                    default:
                        MsgTypeResp = MsgTypeReq;
                        break;
                }
            }
            else
            {
                MsgTypeResp = MsgTypeReq;
            }

            return MsgTypeResp;
        }

        public static long GetAmount(string stramt, string strdp)
        {
            int dp = NbConvert.ToInt(strdp);

            return NbConvert.ToLong(stramt.Substring(0, stramt.Length - dp));
        }
        public static long GetAmount(string stramt, int dp)
        {
            return NbConvert.ToLong(stramt.Substring(0, stramt.Length - dp));
        }
        public static long GetAmount(string amt)
        {
            amt = amt.Replace(",", "");
            amt = amt.Replace(".", "");

            return NbConvert.ToLong(amt);
        }

        public static string PanMasking(string pan)
        {
            string ret = "";

            if (pan.Length == 16)
                ret = pan.Substring(0, 6) +
                      "".PadRight(6, '*') +
                      pan.Substring(pan.Length - 4, 4);
            else
                ret = "".PadLeft(pan.Length, '*');

            return ret;
        }
        public static string GetPANFromTrack2Data(string track2data)
        {
            if (track2data == null)
                return "";

            int i = (track2data.ToLower()).IndexOf("d") + 1;

            if (i == 0)
                i = track2data.IndexOf("=") + 1;

            return track2data.Substring(0, i - 1);
        }

        public static string SetStructuredData(string sKey, string sValue)
        {
            string Result = "";

            try
            {
                string KeyLength = sKey.Length.ToString();
                string KeyLengthIndicator = KeyLength.Length.ToString();

                string ValueLength = sValue.Length.ToString();
                string ValueLengthIndicator = ValueLength.Length.ToString();

                Result = KeyLengthIndicator + KeyLength + sKey + ValueLengthIndicator + ValueLength + sValue;
            }
            catch (Exception)
            {

            }

            return Result;
        }
        public static string GetStructuredData(string sKey, string sData)
        {
            string ValueName = "";

            try
            {
                int StartPosition = sData.IndexOf(sKey) + 1 + sKey.Length;

                if (StartPosition == sKey.Length)
                {
                    //data is not found
                }
                else
                {
                    int ValueLengthIndicator = Convert.ToInt32(sData.Substring(StartPosition - 1, 1));
                    int ValueLength = Convert.ToInt32(sData.Substring(StartPosition + 1 - 1, ValueLengthIndicator));

                    ValueName = sData.Substring(StartPosition + ValueLengthIndicator + 1 - 1, ValueLength);
                }
            }
            catch (Exception)
            {

            }

            return ValueName;
        }
        public static string RemoveStructuredData(string sKey, string sData)
        {
            string result = sData;

            try
            {
                int StartPosition = sData.IndexOf(sKey) + 1 + sKey.Length;

                int ValueLengthIndicator = Convert.ToInt32(sData.Substring(StartPosition - 1, 1));
                int ValueLength = Convert.ToInt32(sData.Substring(StartPosition + 1 - 1, ValueLengthIndicator));
                string ValueName = sData.Substring(StartPosition - ValueLengthIndicator - sKey.Length - 1, ValueLength + ValueLengthIndicator + sKey.Length + ValueLength.ToString().Length + ValueLengthIndicator.ToString().Length);

                result = sData.Replace(ValueName, "");
            }
            catch (Exception)
            {

            }

            return result;
        }

        public static string GetSwitchKey(string MsgType, string[] DE)
        {
            return MsgType.Substring(1, 1) +
                    DE[7] +
                    DE[11] +
                    DE[41];
        }
        public static string GetSwitchKeyOriginal(string[] DE)
        {
            /* field 90
             * -------------------
             * message type     4
             * trace number     6
             * date time        10
             * acq inst id      11
             * fwd inst id      11
             */

            string returnValue = "2" +
                DE[90].Substring(10, 10) + //datetime
                DE[90].Substring(4, 6) + //trace
                DE[41];                  //tid

            return returnValue;
        }

        public static bool IsRequestMessage(string MsgType)
        {
            bool bval = false;

            if (string.IsNullOrEmpty(MsgType) == false)
            {
                if (MsgType.Substring(2, 1) == "0" ||
                    MsgType.Substring(2, 1) == "2")
                {
                    bval = true;
                }
            }

            return bval;
        }
        public static bool IsResponseMessage(string MsgType)
        {
            bool bval = false;

            if (string.IsNullOrEmpty(MsgType) == false)
            {
                if (MsgType.Substring(2, 1) == "1" ||
                    MsgType.Substring(2, 1) == "3")
                {
                    bval = true;
                }
            }

            return bval;
        }

        public static string ContructXmlMessage(string[] de)
        {
            string ret = "<Iso8583Xml>";

            for (int i = 0; i < de.Length; i++)
            {
                if (string.IsNullOrEmpty(de[i]) == false)
                {
                    ret = ret +
                        "<F" + i.ToString() + ">" +
                        de[i] +
                        "</F" + i.ToString() + ">";
                }
            }

            ret = ret + "</Iso8583Xml>";

            return ret;
        }

        public static string[] ExtractXmlMessage(string xmlmessage)
        {
            string[] de = new string[129];

            string tagStart;
            string tagEnd;

            int idxStart;
            int idxEnd;

            try
            {
                //validate
                if (string.IsNullOrEmpty(xmlmessage)) return de;

                //extract
                for (int i = 0; i < de.Length; i++)
                {
                    tagStart = "<F" + i.ToString() + ">";
                    tagEnd = "</F" + i.ToString() + ">";

                    idxStart = xmlmessage.IndexOf(tagStart);
                    idxEnd = xmlmessage.IndexOf(tagEnd);

                    if (idxStart > -1 && idxEnd > -1)
                    {
                        idxStart = idxStart + tagStart.Length;
                        idxEnd = idxEnd - idxStart;

                        de[i] = xmlmessage.Substring(idxStart, idxEnd);
                    }
                    else
                        de[i] = "";
                }
            }
            catch { }

            return de;
        }
    }
}
