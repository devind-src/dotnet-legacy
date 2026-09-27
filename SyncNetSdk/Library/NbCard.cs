using SyncNet.Cryptography;
using System;
using System.Text;

namespace SyncNet.Library
{
    public class NbCard
    {
        public enum EnumPinblockFormat
        {
            FORMAT_01_ANSI = 1,
            FORMAT_02_DOCUTEL = 2,
            FORMAT_03_IBM = 3,
            FORMAT_04_PLUS = 4
        }


        public static string CreatePinBlock(string pan, string pin, string key, EnumPinblockFormat format, out string err_message)
        {
            err_message = ""; //no error

            //validate
            if (pin.Length < 4 || pin.Length > 12)
                err_message = "The Length of PIN must be 4-12 digit.";

            if (pan.Length < 16 || pan.Length > 19)
                err_message = "The Length of PAN must be 16-19 digit.";

            if (NbSystem.IsNumeric(pin) == false)
                err_message = "PIN must be numeric only.";

            if (NbSystem.IsNumeric(pan) == false)
                err_message = "PAN must be numeric only.";

            string clear;
            string xpin;
            string xpan;

            switch (format)
            {
                case EnumPinblockFormat.FORMAT_01_ANSI:
                    pin = "0" + pin.Length + pin;
                    xpin = pin.PadRight(16, 'F');
                    xpan = "0000" + pan.Substring(3, 12);

                    //0-xor,1-or,2-and
                    clear = NbMath.BitwiseOperation(xpin, xpan, NbMath.EnumBitwise.XOR);
                    break;
                case EnumPinblockFormat.FORMAT_02_DOCUTEL:
                    if (pin.Length > 6) pin = pin.Substring(0, 6);

                    clear = pin.Length + pin.PadRight(6, '0') + "987654321";
                    break;
                case EnumPinblockFormat.FORMAT_03_IBM:
                    clear = pin.PadRight(16, 'F');
                    break;
                case EnumPinblockFormat.FORMAT_04_PLUS:
                    pin = "0" + pin.Length + pin;
                    xpin = pin.PadRight(16, 'F');
                    xpan = "0000" + pan.Substring(0, 12);

                    clear = NbMath.BitwiseOperation(xpin, xpan, NbMath.EnumBitwise.XOR);
                    break;
                default:
                    clear = "0000000000000000";
                    break;
            }

            //output pinblock
            return DesAlgorithm.EncryptHex(clear, key);
        }

        public static string GetKeyCheckValue(string key)
        {
            return DesAlgorithm.EncryptHex("".PadLeft(16, '0'), key);
        }

        public static int GetLuhnCheckDigit(string pan)
        {
            int[,] table = new int[2, 10] { { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, { 0, 2, 4, 6, 8, 1, 3, 5, 7, 9 } };

            int size = pan.Length;
            int sum = 0;
            int odd = 0;

            for (int i = size - 1; i >= 0; i--)
            {
                if (Char.IsDigit(pan[i]))
                {
                    sum += table[odd = 1 - odd, pan[i] - '0'];
                }
            }

            sum %= 10;

            return (sum != 0 ? 10 - sum : 0);
        }

        public static bool IsCreditCardValid(string pan)
        {
            const string allowed = "0123456789";
            int i;

            StringBuilder cleanNumber = new StringBuilder();
            for (i = 0; i < pan.Length; i++)
            {
                if (allowed.IndexOf(pan.Substring(i, 1)) >= 0)
                    cleanNumber.Append(pan.Substring(i, 1));
            }
            if (cleanNumber.Length < 13 || cleanNumber.Length > 16)
                return false;

            for (i = cleanNumber.Length + 1; i <= 16; i++)
                cleanNumber.Insert(0, "0");

            int multiplier, digit, sum, total = 0;
            string number = cleanNumber.ToString();

            for (i = 1; i <= 16; i++)
            {
                multiplier = 1 + (i % 2);
                digit = int.Parse(number.Substring(i - 1, 1));
                sum = digit * multiplier;
                if (sum > 9)
                    sum -= 9;
                total += sum;
            }
            return (total % 10 == 0);
        }

        public static string SetCheckDigit(string pan)
        {
            int chk = GetLuhnCheckDigit(pan);

            return pan + chk.ToString();
        }
    }
}
