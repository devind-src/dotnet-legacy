using System;
using System.Globalization;

namespace SyncNet.Library
{
    public class NbSystem
    {
        private static Random _rnd = new Random();

        private static int _CurrentTraceNumber;
        private static int _CurrentReceiptNumber;

        public static string getFolderProgramFiles()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        }

        public static string getMonthName(int month)
        {
            string ret = "";

            if (month >= 1 && month <= 12)
                ret = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

            return ret;
        }

        public static string getMonthName(DateTime dateTime)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(dateTime.Month);
        }

        public static string getShortMonthName(int month)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month);
        }

        public static string getShortMonthName(DateTime dateTime)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(dateTime.Month);
        }

        public static bool IsNumeric(string strnum)
        {
            if (string.IsNullOrEmpty(strnum) == true) return false;

            bool bval = true;

            try
            {
                for (int i = 0; i < strnum.Length; i++)
                {
                    string s = strnum.Substring(i, 1);

                    Int32.Parse(s);
                }
            }
            catch
            {
                bval = false;
            }

            return bval;
        }

        public static void ResizeArray(ref string[,] original, int rows, int cols)
        {
            //create a new 2 dimensional array with
            //the size we want
            string[,] newArray = new string[rows, cols];
            //copy the contents of the old array to the new one
            Array.Copy(original, newArray, original.Length);
            //set the original to the new array
            original = newArray;
        }

        public static string setlVar(string de, int lenvar)
        {
            string result = "";

            if (string.IsNullOrEmpty(de) == true)
                result = de;
            else
                result = de.Length.ToString().PadLeft(lenvar, '0') + de;

            return result;
        }

        public static string FormatKey(string key)
        {
            string result = key;

            if (key.Length == 16)
                result =
                    key.Substring(0, 4) + "-" +
                    key.Substring(4, 4) + "-" +
                    key.Substring(8, 4) + "-" +
                    key.Substring(12, 4);

            return result;
        }

        public static string FormatNumber(int amt)
        {
            return FormatAmount(amt.ToString());
        }

        public static string FormatNumber(long amt)
        {
            return FormatAmount(amt.ToString());
        }

        public static string FormatNumber(string amt)
        {
            return FormatAmount(amt);
        }

        public static string FormatAmount(int amt)
        {
            return FormatAmount(amt.ToString());
        }

        public static string FormatAmount(long amt)
        {
            return FormatAmount(amt.ToString());
        }

        public static string FormatAmount(string amt)
        {
            amt = amt.Replace(",", "");
            amt = amt.Replace(".", "");

            long iamt = NbConvert.ToLong(amt);
            string ret = iamt.ToString("##,##");

            if (ret == "")
                ret = "0";

            return ret;
        }

        public static long getAmount(string stramt, string strdp)
        {
            int dp = NbConvert.ToInt(strdp);

            return NbConvert.ToLong(stramt.Substring(0, stramt.Length - dp));
        }

        public static long getAmount(string stramt, int dp)
        {
            return NbConvert.ToLong(stramt.Substring(0, stramt.Length - dp));
        }

        public static long getAmount(string amt)
        {
            amt = amt.Replace(",", "");
            amt = amt.Replace(".", "");

            return NbConvert.ToLong(amt);
        }

        public static string getMsgTypeResp(string MsgTypeReq)
        {
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

        public static string getPANFromTrack2Data(string track2data)
        {
            if (track2data == null)
                return "";

            int i = (track2data.ToLower()).IndexOf("d") + 1;

            if (i == 0)
                i = track2data.IndexOf("=") + 1;

            return track2data.Substring(0, i - 1);
        }

        public static string setStructuredData(string sKey, string sValue)
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

        public static string getStructuredData(string sKey, string sData)
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

        public static string getNodeTypeDescription(byte NodeType)
        {
            string nm = "";

            if (NodeType == 0)
                nm = "(source)";
            else
                nm = "(sink)";

            return nm;
        }

        public static string getSwitchKey(string MsgType, string[] DE)
        {
            return MsgType.Substring(1, 1) +
                    DE[7] +
                    DE[11] +
                    DE[41];
        }

        public static string getOriginalSwitchKey(string[] DE)
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

        public static string getRandomHexNumber(int len)
        {
            char[] arrData = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
            string ret = "";
            int idx;

            for (int i = 0; i < len; i++)
            {
                idx = _rnd.Next(16);

                ret = ret + arrData[idx];
            }

            return ret;
        }

        public static string getRandomNumber(int len)
        {
            char[] arrData = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            string ret = "";
            int idx;

            for (int i = 1; i <= len; i++)
            {
                idx = _rnd.Next(10);

                ret = ret + arrData[idx];
            }

            return ret;
        }

        public static string getRandomNumber()
        {
            return _rnd.Next(99999).ToString();
        }

        public static string getTraceNumber()
        {
            _CurrentTraceNumber++;

            //reset number
            if (_CurrentTraceNumber >= 999999)
                _CurrentTraceNumber = 1;

            string mTraceNumber = _CurrentTraceNumber.ToString();

            return mTraceNumber.PadLeft(6, '0');
        }

        public static string getReceiptNumber()
        {
            _CurrentReceiptNumber++;

            string mReceiptNumber = _CurrentReceiptNumber.ToString();

            return mReceiptNumber.PadLeft(12, '0');
        }

        public static string getTime()
        {
            return DateTime.Now.ToString("HHmmss");
        }

        public static string getDate()
        {
            return DateTime.Now.ToString("MMdd");
        }

        public static string getTransDateTime()
        {
            return DateTime.Now.ToString("MMddHHmmss");
        }

        public static void StartRCPTNumber(int Number)
        {
            if (Number == 0)
                _CurrentReceiptNumber = int.Parse(getRandomNumber());
            else
                _CurrentReceiptNumber = Number;
        }

        public static void StartTraceNumber(int Number)
        {
            if (Number == 0)
                _CurrentTraceNumber = int.Parse(getRandomNumber());
            else
                _CurrentTraceNumber = Number;
        }
    }
}
