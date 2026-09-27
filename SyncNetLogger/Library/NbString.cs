using System;

namespace SyncNet.Library
{
    public class NbString
    {
        public static string AscInHex(string s)
        {
            return Hex(Asc(s));
        }

        public static string AscInHex(char c)
        {
            return Hex(Asc(c));
        }

        public static byte Asc(string s)
        {
            return Convert.ToByte(s);
        }

        public static byte Asc(char c)
        {
            return Convert.ToByte(c);
        }

        public static char Chr(int i)
        {
            return (char)i;
        }

        public static char Chr(byte b)
        {
            return (char)b;
        }

        public static string Hex(byte num)
        {
            return num.ToString("X").PadLeft(2, '0');
        }

        public static string Hex(int num)
        {
            return num.ToString("X").PadLeft(2, '0');
        }
    }
}
