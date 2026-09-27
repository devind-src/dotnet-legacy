using SyncNet.Cryptography;
using SyncNet.Library;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SyncNet.Common
{
    internal class LicenseManager
    {
        public static string GetSerialNumber()
        {
            string retval = "";

            try
            {
                //check platform
                bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                string sndisk;

                if (IsWindows == true)
                    sndisk = ServerInfo.GetDiskInfoWindows();
                else
                    sndisk = ServerInfo.GetDiskInfoLinux();

                retval = HashProvider.ComputeMd5Hash(sndisk);
            }
            catch { }

            return retval;
        }

        public static bool ValidateLicense()
        {
            bool retval = false;

            try
            {
                //check license key
                string LicenseKey = AppConfig.LicenseKey;
                if (string.IsNullOrEmpty(LicenseKey) == true) return retval;

                //flip
                string flip = LicenseKey.Substring(LicenseKey.Length / 2) +
                    LicenseKey.Substring(0, LicenseKey.Length / 2);

                byte[] bytes = NbConvert.HexToBytes(flip);

                string plain = Decrypt(bytes);
                if (string.IsNullOrEmpty(plain) == true) return retval;

                //validate data license
                string licenseData = NbConvert.BytesToHex(Convert.FromBase64String(plain));
                if (string.IsNullOrEmpty(licenseData) == true) return retval;
                if (licenseData.Length < 10) return retval;

                //validate server id
                string serverId = licenseData.Substring(0, licenseData.Length - 8);
                string SerialNumber = GetSerialNumber();
                if (serverId != SerialNumber) return retval;

                //get date expired
                string dtExpired = licenseData.Substring(licenseData.Length - 8);
                if (IsDate(dtExpired) == false) return retval;

                //validate expired date
                int iDtExpired = NbConvert.ToInt(dtExpired);
                int dtNow = NbConvert.ToInt(DateTime.Now.ToString("yyyyMMdd"));
                if (iDtExpired > dtNow) retval = true;
            }
            catch { }

            return retval;
        }

        private static string Decrypt(byte[] cipherBytes)
        {
            string key = "4I9D6dfx9tww+J7NyP3KZ+jD4Vq6SsPmmZvNAcPgUoU=";
            string iv = "tDr5qdXokODro+U75VsWTw==";
            string plaintext;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Convert.FromBase64String(key);
                aesAlg.IV = Convert.FromBase64String(iv);

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }

        private static bool IsDate(string value)
        {
            bool retval = false;

            try
            {
                if (string.IsNullOrEmpty(value) == true) return false;
                if (value.Length != 8) return false;

                string dateString =
                    value.Substring(0, 4) + "-" +
                    value.Substring(4, 2) + "-" +
                    value.Substring(6, 2);

                string format = "yyyy-MM-dd";
                DateTime dateTime = DateTime.ParseExact(dateString, format, null);

                retval = true;
            }
            catch { }

            return retval;
        }
    }
}
