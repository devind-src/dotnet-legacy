using System;

namespace SyncNet.Library
{
    public class NbSystem
    {
        private static Random _rnd = new Random();

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
    }
}
