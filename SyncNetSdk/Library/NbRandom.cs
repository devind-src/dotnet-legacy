using System;
using System.Text;

namespace SyncNet.Library
{
    public static class NbRandom
    {
        private static readonly Random _rnd = new();

        public static string GenerateHex(int length)
        {
            if (length <= 0) throw new ArgumentException("Length harus > 0");

            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                int val = _rnd.Next(0, 16); // digit 0-F
                sb.Append(val.ToString("X")); // hex uppercase
            }
            return sb.ToString();
        }
        public static string GenerateNumber(int length)
        {
            if (length <= 0) throw new ArgumentException("Length harus > 0");

            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(_rnd.Next(0, 10)); // digit 0-9
            }
            return sb.ToString();
        }
        public static string GenerateString(int length)
        {
            if (length <= 0) throw new ArgumentException("Length harus > 0");

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                int idx = _rnd.Next(chars.Length);
                sb.Append(chars[idx]);
            }
            return sb.ToString();
        }
        public static string GenerateCaptcha(int length)
        {
            if (length <= 0) throw new ArgumentException("Length harus > 0");

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                int idx = _rnd.Next(chars.Length);
                sb.Append(chars[idx]);
            }
            return sb.ToString();
        }
        public static string GeneratePassword(int length)
        {
            if (length <= 0) throw new ArgumentException("Length harus > 0");

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*_-+=";
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                int idx = _rnd.Next(chars.Length);
                sb.Append(chars[idx]);
            }
            return sb.ToString();
        }
    }
}
