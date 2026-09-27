using SyncNet.Library;
using System.Security.Cryptography;

namespace SyncNet.Cryptography
{
    public class HashProvider
    {
        public static string ComputeMd5Hash(string message)
        {
            using (MD5 hash = MD5.Create())
            {
                byte[] bytes = hash.ComputeHash(NbConvert.StringToBytes(message));

                return NbConvert.BytesToHex(bytes);
            }
        }
        public static string ComputeSha1Hash(string message)
        {
            using (SHA1 shaHash = SHA1.Create())
            {
                byte[] bytes = shaHash.ComputeHash(NbConvert.StringToBytes(message));

                return NbConvert.BytesToHex(bytes);
            }
        }
        public static string ComputeSha256Hash(string message)
        {
            using (SHA256 shaHash = SHA256.Create())
            {
                byte[] bytes = shaHash.ComputeHash(NbConvert.StringToBytes(message));

                return NbConvert.BytesToHex(bytes);
            }
        }
        public static string ComputeSha384Hash(string message)
        {
            using (SHA384 shaHash = SHA384.Create())
            {
                byte[] bytes = shaHash.ComputeHash(NbConvert.StringToBytes(message));

                return NbConvert.BytesToHex(bytes);
            }
        }
        public static string ComputeSha512Hash(string message)
        {
            using (SHA512 shaHash = SHA512.Create())
            {
                byte[] bytes = shaHash.ComputeHash(NbConvert.StringToBytes(message));

                return NbConvert.BytesToHex(bytes);
            }
        }
        public static byte[] ComputeHMACMD5Hash(string message, string secret_key)
        {
            byte[] key = NbConvert.StringToBytes(secret_key);
            byte[] source = NbConvert.StringToBytes(message);

            var hash = new HMACMD5(key);

            return hash.ComputeHash(source);
        }
        public static byte[] ComputeHMACSHA1Hash(string message, string secret_key)
        {
            byte[] key = NbConvert.StringToBytes(secret_key);
            byte[] source = NbConvert.StringToBytes(message);

            var hash = new HMACSHA1(key);

            return hash.ComputeHash(source);

        }
        public static byte[] ComputeHMACSHA256Hash(string message, string secret_key)
        {
            byte[] key = NbConvert.StringToBytes(secret_key);
            byte[] source = NbConvert.StringToBytes(message);

            var hash = new HMACSHA256(key);

            return hash.ComputeHash(source);
        }
        public static byte[] ComputeHMACSHA384Hash(string message, string secret_key)
        {
            byte[] key = NbConvert.StringToBytes(secret_key);
            byte[] source = NbConvert.StringToBytes(message);

            var hash = new HMACSHA384(key);

            return hash.ComputeHash(source);
        }
        public static byte[] ComputeHMACSHA512Hash(string message, string secret_key)
        {
            byte[] key = NbConvert.StringToBytes(secret_key);
            byte[] source = NbConvert.StringToBytes(message);

            var hash = new HMACSHA512(key);

            return hash.ComputeHash(source);
        }
    }
}
