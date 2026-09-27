using System;

namespace SyncNet.Library
{
    public class NbMath
    {
        public enum EnumBitwise
        {
            XOR = 0,
            OR = 1,
            AND = 2
        }

        public static string HexToBin(string shex)
        {
            string sbinner = "";
            string result = "";
            string ichar;

            for (int I = 1; I <= shex.Length; I++)
            {
                ichar = shex.Substring(I - 1, 1);

                switch (ichar.ToUpper())
                {
                    case "0":
                        sbinner = "0000";
                        break;
                    case "1":
                        sbinner = "0001";
                        break;
                    case "2":
                        sbinner = "0010";
                        break;
                    case "3":
                        sbinner = "0011";
                        break;
                    case "4":
                        sbinner = "0100";
                        break;
                    case "5":
                        sbinner = "0101";
                        break;
                    case "6":
                        sbinner = "0110";
                        break;
                    case "7":
                        sbinner = "0111";
                        break;
                    case "8":
                        sbinner = "1000";
                        break;
                    case "9":
                        sbinner = "1001";
                        break;
                    case "A":
                        sbinner = "1010";
                        break;
                    case "B":
                        sbinner = "1011";
                        break;
                    case "C":
                        sbinner = "1100";
                        break;
                    case "D":
                        sbinner = "1101";
                        break;
                    case "E":
                        sbinner = "1110";
                        break;
                    case "F":
                        sbinner = "1111";
                        break;
                }

                result = result + sbinner;
            }

            return result;
        }

        public static string BinToHex(string sbin)
        {
            string result = "";
            string bhex = "";
            string ch;

            for (int I = 0; I <= (sbin.Length / 4) - 1; I++)
            {
                ch = sbin.Substring(1 + (I * 4) - 1, 4);

                switch (ch)
                {
                    case "0000":
                        bhex = "0";
                        break;
                    case "0001":
                        bhex = "1";
                        break;
                    case "0010":
                        bhex = "2";
                        break;
                    case "0011":
                        bhex = "3";
                        break;
                    case "0100":
                        bhex = "4";
                        break;
                    case "0101":
                        bhex = "5";
                        break;
                    case "0110":
                        bhex = "6";
                        break;
                    case "0111":
                        bhex = "7";
                        break;
                    case "1000":
                        bhex = "8";
                        break;
                    case "1001":
                        bhex = "9";
                        break;
                    case "1010":
                        bhex = "A";
                        break;
                    case "1011":
                        bhex = "B";
                        break;
                    case "1100":
                        bhex = "C";
                        break;
                    case "1101":
                        bhex = "D";
                        break;
                    case "1110":
                        bhex = "E";
                        break;
                    case "1111":
                        bhex = "F";
                        break;
                }
                result = result + bhex;
            }

            return result;
        }

        public static int BinToDec(string sbin)
        {
            int ret = 0;
            int dec;

            string ch;

            for (int i = 1; i <= sbin.Length; i++)
            {
                ch = sbin.Substring(i - 1, 1);
                dec = Convert.ToInt32(int.Parse(ch) * (Math.Pow(2, (sbin.Length - i))));
                ret = ret + dec;
            }

            return ret;
        }

        public static string DecToBin(long Number)
        {
            string result = "";

            if (Math.Abs(Number) > 1)
            {
                result = DecToBin(Number / 2) + Math.Abs(Number % 2).ToString();
            }
            else
            {
                result = Number.ToString();
            }

            return result;
        }

        public static int HexToDec(string shex)
        {
            int result = 0;
            string bin;

            bin = HexToBin(shex);
            result = BinToDec(bin);

            return result;
        }

        public static string BitwiseOperation(string FirstValue, string SecondValue, EnumBitwise Operator)
        {
            string bin_val1;
            string bin_val2;
            string cTemp = "";

            byte a = 0;
            byte b = 0;
            int c = 0;

            bin_val1 = HexToBin(FirstValue);
            bin_val2 = HexToBin(SecondValue);

            for (int i = 1; i <= bin_val1.Length; i++)
            {
                try
                {
                    a = Convert.ToByte(bin_val1.Substring(i - 1, 1));
                    b = Convert.ToByte(bin_val2.Substring(i - 1, 1));
                }
                catch { }

                switch (Operator)
                {
                    case EnumBitwise.XOR:
                        c = a ^ b;
                        break;
                    case EnumBitwise.OR:
                        c = a | b;
                        break;
                    case EnumBitwise.AND:
                        c = a & b;
                        break;
                }

                cTemp = cTemp + c;
            }

            return BinToHex(cTemp);
        }
    }
}
