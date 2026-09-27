using ARNSdk.Library;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ARNSdk.Common
{
    class Signature
    {
        private static string _mk;

        public static void Resync()
        {
            NbConfig cfg = new NbConfig();
            _mk = cfg.GetValueDecrypted("SecretKey", "607bb6ec07604b0dc3cf9d0c0c8e0844a482525d1e34f66b");
        }

        public static bool IsValid(string signature, string body)
        {
            string hash = getHashHMAC(body);

            if (signature == hash)
                return true;
            else
                return false;
        }

        public static string getHashHMAC(string message)
        {
            byte[] bkey = Encoding.ASCII.GetBytes(_mk);
            byte[] bmessage = Encoding.ASCII.GetBytes(message);

            byte[] bytes = HashHMAC(bkey, bmessage);

            return Convert.ToBase64String(bytes);
        }

        private static byte[] HashHMAC(byte[] key, byte[] message)
        {
            var hash = new HMACSHA256(key);

            return hash.ComputeHash(message);
        }
    }
}
