namespace SyncNet.Library
{
    public class NbFormat
    {
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

        public static string FormatBinary(byte[] bytes)
        {
            //string strdata = Encoding.ASCII.GetString(bytes);
            string strdata = NbConvert.BytesToString(bytes);

            return FormatBinary(strdata);
        }
        public static string FormatBinary(string strdata)
        {
            const short LENGTH_ROW = 16;

            int lengthRow = strdata.Length;
            int maxLoop = lengthRow / LENGTH_ROW;

            string ret = "";
            string tmp = "";

            string sbin;
            string shex;

            for (int i = 0; i <= maxLoop; i++)
            {
                if (i == maxLoop)
                    sbin = strdata.Substring(i * LENGTH_ROW, lengthRow - (i * LENGTH_ROW));
                else
                    sbin = strdata.Substring(i * LENGTH_ROW, LENGTH_ROW);

                shex = "";

                for (int j = 0; j < sbin.Length; j++)
                {
                    tmp = sbin.Substring(j, 1);
                    tmp = NbString.AscInHex(tmp);

                    shex = shex + " " + tmp;
                }

                //display binary + hex
                if (sbin.Length < 16)
                    sbin = sbin + "".PadLeft(16 - sbin.Length, ' ');

                ret = ret + "[" + (i * LENGTH_ROW).ToString().PadLeft(5, '0') + "]  " + FormatString(sbin) + "  " + shex + "\r\n";
            }

            return ret;
        }
        public static string FormatString(string strdata)
        {
            string ret = "";

            for (int i = 0; i < strdata.Length; i++)
            {
                if (NbString.Asc(strdata.Substring(i, 1)) < 32)
                    ret = ret + ".";
                else if (NbString.Asc(strdata.Substring(i, 1)) > 126)
                    ret = ret + ".";
                else
                    ret = ret + strdata.Substring(i, 1);
            }

            return ret;
        }
    }
}
